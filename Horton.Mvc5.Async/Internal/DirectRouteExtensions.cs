﻿// Source copied from: https://aspnetwebstack.codeplex.com/SourceControl/changeset/view/5fa60ca38b5837cc843b5d4552113f9a0235c3bf#src/System.Web.Mvc/Routing/DirectRouteExtensions.cs
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
//
// https://aspnetwebstack.codeplex.com/SourceControl/changeset/view/5fa60ca38b5837cc843b5d4552113f9a0235c3bf#License.txt
//
// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.
// Microsoft Open Technologies would like to thank its contributors, a list
// of whom are at http://aspnetwebstack.codeplex.com/wikipage?title=Contributors.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License. You may
// obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied. See the License for the specific language governing permissions
// and limitations under the License.

using System.Web.Routing;

namespace System.Web.Mvc.Routing
{
    internal static class DirectRouteExtensions
    {
        public static bool HasDirectRouteMatch(this RouteData routeData)
        {
            if (routeData == null)
            {
                throw new ArgumentNullException(nameof(routeData));
            }
            return routeData.Values.ContainsKey(RouteDataTokenKeys.DirectRouteMatches);
        }
    }
}
