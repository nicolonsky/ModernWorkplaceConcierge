// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(IntuneConcierge.Startup))]

namespace IntuneConcierge
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}