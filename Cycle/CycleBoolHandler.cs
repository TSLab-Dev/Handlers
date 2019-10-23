namespace TSLab.Script.Handlers
{
    [HandlerInvisible]
    [OutputType(TemplateTypes.BOOL)]
    public abstract class CycleBoolHandler : IValuesHandlerWithNumber, IOneSourceHandler, IBooleanInputs, IBooleanReturns
    {
        private int m_barIndex = -1;
        private bool m_result;
        private bool m_value;

        public bool Execute(bool value, int barIndex)
        {
            if (m_barIndex != barIndex)
            {
                m_barIndex = barIndex;
                m_result = m_value;
                m_value = value;
            }
            else
                m_value = Execute(m_value, value);

            return m_result;
        }

        protected abstract bool Execute(bool value1, bool value2);
    }

    public sealed class CycleAndHandler : CycleBoolHandler
    {
        protected override bool Execute(bool value1, bool value2) => value1 && value2;
    }

    public sealed class CycleOrHandler : CycleBoolHandler
    {
        protected override bool Execute(bool value1, bool value2) => value1 || value2;
    }
}
