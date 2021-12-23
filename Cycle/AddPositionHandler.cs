using System;
using System.Collections.Generic;
using System.Text;
using TSLab.Script.Handlers;

namespace TSLab.ScriptEngine.Template
{
    [HandlerInvisible]
    [OutputsCount(0)]
    public sealed class AddPositionHandler : IHandler
    {
        [HandlerParameter(true, "false", NotOptimized = true)]
        public bool UseVirtualClosing { get; set; }
    }
}
