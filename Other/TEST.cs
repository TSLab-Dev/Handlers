using System;
using System.ComponentModel;

using TSLab.DataSource;
using TSLab.Script.CanvasPane;
using TSLab.Script.Handlers.Options;
using TSLab.Script.Optimization;

#if DEBUG
namespace TSLab.Script.Handlers
{
    [HandlerCategory(BlockCategories.CanvasPaneHandler)]
    [HelperName("Test Canvas Pane Handler", Language = Constants.En)]
    [HelperName("Test Canvas Pane Handler", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.CANVASPANE, Name = BlockCategories.CanvasPaneHandler)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.CANVASPANE)]
    [Description("Тестовый кубик для проверки работы с CanvasPane. Виден только в девелоперской версии.")]
    [HelperDescription("Test handler to check interaction with a CanvasPane. It is visible in developer build only.", Constants.En)]
#if !DEBUG
    [HandlerInvisible]
#endif
    public sealed class TestCanvasPaneHandler : ITestCanvasPaneHandler
    {
        /// <summary>
        /// \~english Value of a constant
        /// \~russian Значение константы
        /// </summary>
        [HelperName("Value", Constants.En)]
        [HelperName("Значение", Constants.Ru)]
        [Description("Значение константы")]
        [HelperDescription("Value of a constant", Constants.En)]
        [HandlerParameter(Name = "Value", Default = "100")]
        public OptimProperty Value { get; set; }

        public ICanvasPane Execute(ICanvasPane pane)
        {
            return pane ?? throw new ArgumentNullException(nameof(pane));
        }
    }
}
#endif