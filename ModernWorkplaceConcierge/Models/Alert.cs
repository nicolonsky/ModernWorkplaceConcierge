// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace ModernWorkplaceConcierge.Models
{
    // Used to flash error messages in the app's views.
    public class Alert
    {
        public const string AlertKey = "TempDataAlerts";
        public string Message { get; set; }
        public string Debug { get; set; }
    }

    public class Info
    {
        public const string SessKey = "TempDataInfo";
        public string Message { get; set; }
        public string Debug { get; set; }
    }
}