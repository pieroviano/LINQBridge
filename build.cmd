@echo off

REM LINQBridge
REM Copyright (c) 2007 Atif Aziz, Joseph Albahari. All rights reserved.
REM
REM  Author(s):
REM
REM      Atif Aziz, http://www.raboof.com
REM
REM This library is free software; you can redistribute it and/or modify it 
REM under the terms of the New BSD License, a copy of which should have 
REM been delivered along with this distribution.
REM
REM THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 
REM "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
REM LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
REM PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT 
REM OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
REM SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
REM LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, 
REM DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY 
REM THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
REM (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
REM OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
REM
REM -------------------------------------------------------------------------

pushd "%~dp0"
call :main %*
popd
goto:EOF

:main
setlocal
nuget restore && call :build Debug %* && call :build Release %*
goto :EOF

:build
setlocal
"%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\msbuild" "/p:Configuration=%~1" "%~dp0LINQBridge.sln" %2 %3 %4 %5 %6 %7 %8 %9
goto :EOF
