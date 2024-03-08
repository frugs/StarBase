namespace PlayerDB.Utilities;

public static class LinqExtensions
{
    public static List<T> FoldToList<T>(this IEnumerable<IEnumerable<T>> nestedEnumerable)
    {
        return nestedEnumerable.SelectMany(x => x).ToList();
    }

    public static void AddAll<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    public static void OverwriteWith<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        collection.AddAll(items);
    }
}