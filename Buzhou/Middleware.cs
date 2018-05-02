using System;

namespace Buzhou
{
    public abstract class Middleware
    {
        /// <summary>
        /// 中间件唯一标识
        /// </summary>
        // ReSharper disable once InconsistentNaming
        internal string ID { get; }

        /// <summary>
        /// 优先级
        /// 优先级高的中间件会先于优先级低的中间件执行
        /// </summary>
        internal int Priority { get; }

        protected Middleware(string id, int priority)
        {
            ID = id;
            Priority = priority;
        }

        public virtual void Setup() { }

        public virtual void TearDown() { }
        
        public virtual void Before(Context context) { }

        public virtual void After(Context context) { }
    }
}
