using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace MvcAsync
{
    /// <summary>
    /// A one-way cursor for filters.
    /// </summary>
    /// <remarks>
    /// This will iterate the filter collection once per-stage, and skip any filters that don't have
    /// the one of interfaces that applies to the current stage.
    ///
    /// Filters are always executed in the following order, but short circuiting plays a role.
    ///
    /// Indentation reflects nesting.
    ///
    /// 1. Exception Filters
    ///     2. Authorization Filters
    ///     3. Action Filters
    ///        Action
    ///
    /// 4. Result Filters
    ///    Result
    ///
    /// </remarks>
    public struct FilterCursor
    {
        // TODO: this is a nasty little hack to get around how Controller is not an IMvcFilter
        private readonly object[] _filters;
        private int _index;

        public FilterCursor(object[] filters)
        {
            _filters = filters;
            _index = 0;
        }

        public FilterCursor(List<object> filters)
        {
            RemoveDuplicates(filters);
            _filters = filters.ToArray();
            _index = 0;
        }

        public void Reset()
        {
            _index = 0;
        }

        public FilterCursorItem<TFilter, TFilterAsync> GetNextFilter<TFilter, TFilterAsync>()
            where TFilter : class
            where TFilterAsync : class
        {
            while (_index < _filters.Length)
            {
                var filter = _filters[_index] as TFilter;
                var filterAsync = _filters[_index] as TFilterAsync;

                _index += 1;

                if (filter != null || filterAsync != null)
                {
                    return new FilterCursorItem<TFilter, TFilterAsync>(filter, filterAsync);
                }
            }

            return default(FilterCursorItem<TFilter, TFilterAsync>);
        }

        private static void RemoveDuplicates(List<object> filters)
        {
            HashSet<Type> visitedTypes = new HashSet<Type>();

            // Remove duplicates from the back forward
            for (int i = filters.Count - 1; i >= 0; i--)
            {
                object filterInstance = filters[i];
                Type filterInstanceType = filterInstance.GetType();

                if (!visitedTypes.Contains(filterInstanceType) || AllowMultiple(filterInstance))
                {
                    visitedTypes.Add(filterInstanceType);
                }
                else
                {
                    filters.RemoveAt(i);
                }
            }
        }

        private static bool AllowMultiple(object filterInstance)
        {
            if (filterInstance is IMvcFilter mvcFilter)
            {
                return mvcFilter.AllowMultiple;
            }
            if (filterInstance is IController)
            {
                return false;
            }
            return true;
        }
    }
}
