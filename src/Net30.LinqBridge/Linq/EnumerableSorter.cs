#nullable disable
namespace System.Linq;

internal abstract class EnumerableSorter<TElement>
{
    internal abstract int CompareKeys(int index1, int index2);
    internal abstract void ComputeKeys(TElement[] elements, int count);

    internal int[] Sort(TElement[] elements, int count)
    {
        ComputeKeys(elements, count);
        var map = new int[count];
        for (var index = 0; index < count; ++index)
        {
            map[index] = index;
        }

        QuickSort(map, 0, count - 1);
        return map;
    }

    private void QuickSort(int[] map, int left, int right)
    {
        do
        {
            var left1 = left;
            var right1 = right;
            var index1 = map[left1 + ((right1 - left1) >> 1)];
            while (true)
            {
                do
                {
                    if (left1 >= map.Length || CompareKeys(index1, map[left1]) <= 0)
                    {
                        while (right1 >= 0 && CompareKeys(index1, map[right1]) < 0)
                        {
                            --right1;
                        }

                        if (left1 <= right1)
                        {
                            if (left1 < right1)
                            {
                                var num = map[left1];
                                map[left1] = map[right1];
                                map[right1] = num;
                            }

                            ++left1;
                            --right1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        goto label_1;
                    }
                } while (left1 <= right1);

                break;
                label_1:
                ++left1;
            }

            if (right1 - left <= right - left1)
            {
                if (left < right1)
                {
                    QuickSort(map, left, right1);
                }

                left = left1;
            }
            else
            {
                if (left1 < right)
                {
                    QuickSort(map, left1, right);
                }

                right = right1;
            }
        } while (left < right);
    }
}