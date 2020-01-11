// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(ModernWorkplaceConcierge.Startup))]

namespace ModernWorkplaceConcierge
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Any connection or hub wire up and configuration should go here
            app.MapSignalR();

            ConfigureAuth(app);
        }
    }
}