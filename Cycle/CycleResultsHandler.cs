using System;
using System.Collections;
using System.Collections.Generic;

namespace TSLab.Script.Handlers
{
    [HandlerInvisible]
    public abstract class CycleResultsHandler<T> : IValuesHandlerWithNumber, IOneSourceHandler, IContextUses
    {
        private sealed class ExReadOnlyList : IList<T>, IReadOnlyList<T>
        {
            private readonly IReadOnlyList<T> m_src;

            public ExReadOnlyList(IReadOnlyList<T> src)
            {
                m_src = src;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return m_src.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(T item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(T item)
            {
                throw new NotSupportedException();
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                throw new NotSupportedException();
            }

            public bool Remove(T item)
            {
                throw new NotSupportedException();
            }

            public int Count => m_src.Count;

            public bool IsReadOnly => true;

            public int IndexOf(T item)
            {
                throw new NotSupportedException();
            }

            public void Insert(int index, T item)
            {
                throw new NotSupportedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotSupportedException();
            }

            public T this[int index]
            {
                get => index < Count ? m_src[index] : default(T);
                set => throw new NotSupportedException();
            }
        }

        private readonly List<IReadOnlyList<T>> m_listOfLists = new List<IReadOnlyList<T>>();
        private readonly List<T> m_list = new List<T>();
        private IContext m_context;

        public IContext Context
        {
            get => m_context;
            set
            {
                m_context = value;
                m_list.Capacity = value.BarsCount;
            }
        }

        public IReadOnlyList<IReadOnlyList<T>> Execute(T value, int barIndex)
        {
            if (m_listOfLists.Count == barIndex)
            {
                if (barIndex > 0)
                    m_listOfLists[barIndex - 1] = new ExReadOnlyList(m_list.ToArray());

                m_list.Clear();
                m_listOfLists.Add(m_list);
            }
            m_list.Add(value);
            return m_listOfLists;
        }
    }

    [OutputType(TemplateTypes.BOOL | TemplateTypes.LIST_OF_LISTS)]
    public sealed class CycleBoolResultsHandler : CycleResultsHandler<bool>, IBooleanInputs
    {
    }

    [OutputType(TemplateTypes.DOUBLE | TemplateTypes.LIST_OF_LISTS)]
    public sealed class CycleDoubleResultsHandler : CycleResultsHandler<double>, IDoubleInputs
    {
    }
}
