namespace ModFramework
{
    public interface ICollection<TItem>
    {
        TItem this[int x, int y] { get; set; }
        int Width { get; }
        int Height { get; }
    }

    public class DefaultCollection<TItem> : ICollection<TItem>
    {
        protected TItem[,]? _items;

        public int Width { get; set; }
        public int Height { get; set; }

        public DefaultCollection(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public virtual TItem this[int x, int y]
        {
            get
            {
                if (_items == null)
                    _items = new TItem[this.Width, this.Height];

                return _items[x, y];
            }
            set => _items![x, y] = value;
        }

        public delegate ICollection<TItem> CreateCollectionHandler(int width, int height, string source);
        public static event CreateCollectionHandler? OnCreateCollection;

        public static ICollection<TItem> CreateCollection(int width, int height, string source)
        {
            var collection = OnCreateCollection?.Invoke(width, height, source) ?? new DefaultCollection<TItem>(width, height);
            System.Console.WriteLine($"Created new {collection.Width}x{collection.Height} {collection.GetType().Name} for source: {source}");
            return collection;
        }
    }
}
