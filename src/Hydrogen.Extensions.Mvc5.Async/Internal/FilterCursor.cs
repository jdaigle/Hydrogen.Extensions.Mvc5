using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Mvc.Filters;

namespace Hydrogen.Extensions.Mvc5.Async.Internal
{
    public struct FilterCursor
    {
        private OverrideFilterInfo _filterInfo;
        private int _index;

        public FilterCursor(IEnumerable<Filter> filters)
        {
            _filterInfo = ProcessOverrideFilters(filters);
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
            while (_index < _filterInfo.Filters.Count)
            {
                var filter = _filterInfo.Filters[_index];

                var filterSync = filter.Instance as TFilter;
                var filterAsync = filter.Instance as TFilterAsync;

                _index += 1;

                if (filterSync != null || filterAsync != null)
                {
                    if (IsOverriden(filter))
                    {
                        continue;
                    }

                    return new FilterCursorItem<TFilter, TFilterAsync>(filterSync, filterAsync);
                }
            }

            return default(FilterCursorItem<TFilter, TFilterAsync>);
        }

        private bool IsOverriden(Filter filter)
        {
            if (filter is IActionFilter && filter.Scope < _filterInfo.ActionOverrideScope)
            {
                return true;
            }
            if (filter is IResultFilter && filter.Scope < _filterInfo.ResultOverrideScope)
            {
                return true;
            }
            if (filter is IAuthenticationFilter && filter.Scope < _filterInfo.AuthenticationOverrideScope)
            {
                return true;
            }
            if (filter is IAuthorizationFilter && filter.Scope < _filterInfo.AuthorizationOverrideScope)
            {
                return true;
            }
            if (filter is IExceptionFilter && filter.Scope < _filterInfo.ExceptionOverrideScope)
            {
                return true;
            }

            return false;
        }

        private static OverrideFilterInfo ProcessOverrideFilters(IEnumerable<Filter> filters)
        {
            OverrideFilterInfo result = new OverrideFilterInfo
            {
                ActionOverrideScope = FilterScope.First,
                AuthenticationOverrideScope = FilterScope.First,
                AuthorizationOverrideScope = FilterScope.First,
                ExceptionOverrideScope = FilterScope.First,
                ResultOverrideScope = FilterScope.First,
                Filters = new List<Filter>()
            };

            // Evaluate the 'filters' enumerable only once since the operation can be quite expensive.
            foreach (Filter filter in filters)
            {
                if (filter == null)
                {
                    continue;
                }
                if (filter.Instance is IOverrideFilter overrideFilter)
                {
                    if (overrideFilter.FiltersToOverride == typeof(IActionFilter)
                        && filter.Scope >= result.ActionOverrideScope)
                    {
                        result.ActionOverrideScope = filter.Scope;
                    }
                    else if (overrideFilter.FiltersToOverride == typeof(IAuthenticationFilter)
                        && filter.Scope >= result.AuthenticationOverrideScope)
                    {
                        result.AuthenticationOverrideScope = filter.Scope;
                    }
                    else if (overrideFilter.FiltersToOverride == typeof(IAuthorizationFilter)
                        && filter.Scope >= result.AuthorizationOverrideScope)
                    {
                        result.AuthorizationOverrideScope = filter.Scope;
                    }
                    else if (overrideFilter.FiltersToOverride == typeof(IExceptionFilter)
                        && filter.Scope >= result.ExceptionOverrideScope)
                    {
                        result.ExceptionOverrideScope = filter.Scope;
                    }
                    else if (overrideFilter.FiltersToOverride == typeof(IResultFilter)
                        && filter.Scope >= result.ResultOverrideScope)
                    {
                        result.ResultOverrideScope = filter.Scope;
                    }
                }

                // Cache filters to avoid having to enumerate it again (expensive). Do so here to avoid an extra loop.
                result.Filters.Add(filter);
            }

            return result;
        }

        private struct OverrideFilterInfo
        {
            public FilterScope ActionOverrideScope;
            public FilterScope AuthenticationOverrideScope;
            public FilterScope AuthorizationOverrideScope;
            public FilterScope ExceptionOverrideScope;
            public FilterScope ResultOverrideScope;

            public List<Filter> Filters;
        }
    }
}
