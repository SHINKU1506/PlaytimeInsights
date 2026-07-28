using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PlaytimeInsights.ViewModels
{
    public class PagedCollection<T>
    {
        private readonly List<T> allItems = new List<T>();

        public PagedCollection(int pageSize)
        {
            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize));
            }

            PageSize = pageSize;
            VisibleItems = new ObservableCollection<T>();
        }

        public int PageSize { get; }

        public int TotalCount => allItems.Count;

        public int VisibleCount => VisibleItems.Count;

        public bool HasMore => VisibleCount < TotalCount;

        public ObservableCollection<T> VisibleItems { get; }

        public void Reset(IEnumerable<T> items)
        {
            allItems.Clear();
            if (items != null)
            {
                allItems.AddRange(items);
            }

            VisibleItems.Clear();
            AppendNextPage();
        }

        public int AppendNextPage()
        {
            var remaining = TotalCount - VisibleCount;
            var count = Math.Min(PageSize, Math.Max(0, remaining));
            foreach (var item in allItems.Skip(VisibleCount).Take(count))
            {
                VisibleItems.Add(item);
            }

            return count;
        }
    }

    public sealed class SessionDetailPager : PagedCollection<SessionDetailViewModel>
    {
        public SessionDetailPager(int pageSize) : base(pageSize)
        {
        }
    }
}
