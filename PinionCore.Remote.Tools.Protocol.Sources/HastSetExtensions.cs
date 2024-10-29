namespace PinionCore.Remote.Tools.Protocol.Sources
{
    public static class HastSetExtensions
    {
        public static int AddRange<T>(this System.Collections.Generic.HashSet<T> set, System.Collections.Generic.IEnumerable<T> items)
        {
            var count = 0;
            foreach (T item in items)
            {
                if (set.Add(item))
                    count++;
            }

            return count;
        }
    }
}
