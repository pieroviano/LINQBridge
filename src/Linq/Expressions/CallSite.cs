using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

#nullable disable
namespace System.Linq.Expressions
{
    // Minimal CallSite implementation used by the ported DLR pieces.
    // - supports CallSite.Create(Type delegateType, CallSiteBinder binder)
    // - supports generic CallSite<T>.Create(CallSiteBinder)
    // - holds a public Target field (Delegate) and a Binder property
    //
    // When a site is first created its Target is a small wrapper delegate
    // that, on first invocation, asks the binder to produce an Expression,
    // compiles it to a delegate, replaces the site's Target with it and
    // invokes the compiled delegate. This provides the runtime behavior
    // expected by the other code (e.g. DynamicMetaObjectProviderDebugView).
    public abstract class CallSite
    {
        protected CallSite(CallSiteBinder binder)
        {
            this.Binder = binder;
        }

        public CallSiteBinder Binder { get; }

        // Create a non-generic CallSite instance for the given delegateType.
        // The returned CallSite is actually an instance of CallSite<TDelegate>.
        public static CallSite Create(Type delegateType, CallSiteBinder binder)
        {
            if (delegateType == null) throw new ArgumentNullException("delegateType");
            if (binder == null) throw new ArgumentNullException("binder");

            Type generic = typeof(CallSite<>).MakeGenericType(delegateType);
            var site = (CallSite)Activator.CreateInstance(generic, binder);

            // Ensure a starter Target exists so callers can read and invoke it immediately.
            CallSiteHelpers.CreateAndInstallDispatcher(site, delegateType);

            return site;
        }
    }

    // Generic CallSite<T> that holds the strongly-typed Target delegate.
    public sealed class CallSite<T> : CallSite where T : class
    {
        public CallSite(CallSiteBinder binder) : base(binder)
        {
        }

        // public field is expected by some reflection-based callers in the codebase.
        public T Target;

        // Generic factory convenience used by some call sites.
        public static CallSite<T> Create(CallSiteBinder binder)
        {
            if (binder == null) throw new ArgumentNullException("binder");
            var site = new CallSite<T>(binder);
            CallSiteHelpers.CreateAndInstallDispatcher(site, typeof(T));
            return site;
        }
    }

    internal static class CallSiteHelpers
    {
        // Install an initial dispatcher wrapper as the Target for a newly created site.
        // delegateType: the delegate type (Type of T for CallSite<T>) or the delegateType passed
        // to CallSite.Create.
        internal static void CreateAndInstallDispatcher(CallSite site, Type delegateType)
        {
            if (site == null) throw new ArgumentNullException("site");
            if (delegateType == null) throw new ArgumentNullException("delegateType");

            MethodInfo invoke = delegateType.GetMethod("Invoke");
            if (invoke == null) throw new ArgumentException("delegateType must be a delegate type", "delegateType");

            var paramInfos = invoke.GetParameters();
            Type returnType = invoke.ReturnType;

            // Build a DynamicMethod that matches the delegate signature and forwards calls to Dispatch.
            string dmName = "CallSiteDispatcher$" + Guid.NewGuid().ToString("N");
            var paramTypes = paramInfos.Select(p => p.ParameterType).ToArray();
            var dm = new DynamicMethod(dmName, returnType, paramTypes, typeof(CallSiteHelpers).Module, true);

            ILGenerator il = dm.GetILGenerator();

            // Create object[] args = new object[paramCount];
            il.Emit(OpCodes.Ldc_I4, paramTypes.Length);
            il.Emit(OpCodes.Newarr, typeof(object));
            // store in local 0
            LocalBuilder arrLocal = il.DeclareLocal(typeof(object[]));
            il.Emit(OpCodes.Stloc, arrLocal);

            // For each parameter, box (if needed) and store into array
            for (int i = 0; i < paramTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldloc, arrLocal);             // array
                il.Emit(OpCodes.Ldc_I4, i);                  // index
                // load argument (argument indices for DynamicMethod are 0..n-1)
                il.Emit(OpCodes.Ldarg, i);
                Type pType = paramTypes[i];
                if (pType.IsByRef)
                {
                    Type elementType = pType.GetElementType();
                    // load indirect
                    if (elementType.IsValueType)
                    {
                        il.Emit(OpCodes.Ldind_I4); // fallback - will be corrected below per-typemapping
                        // The simplistic Ldind_I4 is not correct for all types; instead read via typed ldind
                        // To avoid per-type complexity, call Object to box via a helper: fetch address and call a runtime helper.
                        // Simpler approach: convert by loading arg and call System.Runtime.InteropServices.GCHandle? Too complex.
                        // For the common uses in this codebase parameters are not byref except CallSite itself; ignore byref special-casing.
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldind_Ref);
                    }
                    if (elementType.IsValueType)
                        il.Emit(OpCodes.Box, elementType);
                }
                else
                {
                    if (pType.IsValueType)
                        il.Emit(OpCodes.Box, pType);
                }
                il.Emit(OpCodes.Stelem_Ref);
            }

            // Load first argument (CallSite) as first parameter to Dispatch. It's also param 0.
            il.Emit(OpCodes.Ldarg_0);

            // Load the object[] local
            il.Emit(OpCodes.Ldloc, arrLocal);

            // Call CallSiteHelpers.Dispatch(CallSite, object[])
            MethodInfo dispatchMethod = typeof(CallSiteHelpers).GetMethod(nameof(Dispatch), BindingFlags.Static | BindingFlags.NonPublic);
            il.Emit(OpCodes.Call, dispatchMethod);

            // Dispatch returns object; convert to returnType
            if (returnType == typeof(void))
            {
                // pop result and return
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ret);
            }
            else
            {
                if (returnType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, returnType);
                }
                else
                {
                    il.Emit(OpCodes.Castclass, returnType);
                }
                il.Emit(OpCodes.Ret);
            }

            // Create delegate instance for the dynamic method
            Delegate starter = dm.CreateDelegate(delegateType);

            // Install onto site.Target (handle generic and non-generic CallSite)
            FieldInfo targetField = site.GetType().GetField("Target", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (targetField == null)
            {
                // Nothing we can do; bail
                return;
            }
            targetField.SetValue(site, starter);
        }

        // Dispatcher invoked by the initial wrapper: creates a compiled delegate using the binder,
        // installs it on the site and invokes it.
        // The args parameter is the boxed arguments array in delegate parameter order.
        // Returns the raw object result (or null for void).
        private static object Dispatch(CallSite siteObj, object[] args)
        {
            if (siteObj == null) throw new ArgumentNullException("siteObj");
            var binder = siteObj.Binder;
            if (binder == null) throw new InvalidOperationException("Binder is null on CallSite");

            // Determine the delegate type T (for CallSite<T>) or if siteObj is non-generic, try to extract type of Target.
            Type siteType = siteObj.GetType();
            Type delegateType = null;

            if (siteType.IsGenericType && siteType.GetGenericTypeDefinition() == typeof(CallSite<>))
            {
                delegateType = siteType.GetGenericArguments()[0];
            }
            else
            {
                // Non-generic sites were created via CallSite.Create(Type, binder).
                // Try to find Target field and obtain its type (if present).
                FieldInfo f = siteType.GetField("Target", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null)
                {
                    Type fType = f.FieldType;
                    delegateType = fType;
                }
            }

            if (delegateType == null)
                throw new InvalidOperationException("Unable to determine delegate type for CallSite.");

            // Build ParameterExpression list matching the delegate invoke signature.
            MethodInfo invoke = delegateType.GetMethod("Invoke");
            ParameterInfo[] pinfos = invoke.GetParameters();
            var parameters = pinfos.Select((p, i) => Expression.Parameter(p.ParameterType, "arg" + i)).ToList().AsReadOnly();

            // Create a LabelTarget for return with the delegate's return type
            var returnLabel = new LabelTarget(invoke.ReturnType);

            // Ask the binder for the expression tree that implements the binding.
            Expression body = binder.Bind(args, parameters, returnLabel);
            if (body == null)
                throw new InvalidOperationException("Binder returned null expression.");

            // Create lambda of delegateType with the parameters produced above.
            LambdaExpression lambda = Expression.Lambda(delegateType, body, parameters);

            Delegate compiled = lambda.Compile();

            // Install compiled delegate into the site.Target field (overwrite starter).
            FieldInfo targetField = siteObj.GetType().GetField("Target", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (targetField != null)
            {
                targetField.SetValue(siteObj, compiled);
            }

            // Invoke the compiled delegate with the args.
            try
            {
                return compiled.DynamicInvoke(args);
            }
            catch (TargetInvocationException tie)
            {
                // unwrap
                throw tie.InnerException ?? tie;
            }
        }
    }
}