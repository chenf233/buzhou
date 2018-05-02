using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Buzhou
{
    public class MiddlewareExecutor
    {
        private Dictionary<string, List<Middleware>> _middlewares = new Dictionary<string, List<Middleware>>();

        #region 中间件管理

        private IEnumerable<string> RelevantWorkSets(string workSet)
        {
            return workSet == null ? _middlewares.Keys.ToList() : new List<string> { workSet };
        }

        public void AddMiddleware(Middleware middleware, string workSet = null)
        {
            Debug.Assert(middleware != null);
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            foreach (var ws in RelevantWorkSets(workSet))
            {
                if (!_middlewares.ContainsKey(ws))
                {
                    _middlewares.Add(ws, new List<Middleware>());
                }

                if (_middlewares[ws].Any(m => m.ID == middleware.ID))
                {
                    Debug.Fail($"工作集 '{workSet}' 中已有 ID 为 '{middleware.ID}' 的中间件");
                    throw new Exception($"工作集 '{workSet}' 中已有 ID 为 '{middleware.ID}' 的中间件");
                }

                _middlewares[ws].Add(middleware);
            }
        }

        public void ReplaceMiddleware(Middleware middleware, string workSet = null)
        {
            Debug.Assert(middleware != null);
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            var replaced = 0;
            foreach (var ws in RelevantWorkSets(workSet))
            {
                if (!_middlewares.ContainsKey(ws))
                {
                    Debug.Fail($"工作集 '{ws}' 不存在，无法替换中间件");
                    throw new Exception($"工作集 '{ws}' 不存在，无法替换中间件");
                }

                var index = _middlewares[ws].FindIndex(m => m.ID == middleware.ID);
                if (index == -1)
                {
                    continue;
                }

                _middlewares[ws][index] = middleware;
                replaced++;
            }

            if (replaced == 0)
            {
                Debug.Fail($"没有 ID 为 '{middleware.ID}' 的中间件可供替换");
                throw new Exception($"没有 ID 为 '{middleware.ID}' 的中间件可供替换");
            }
        }

        public void Remove(string id, string workSet = null)
        {
            Debug.Assert(!string.IsNullOrEmpty(id));
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }

            var deleted = 0;
            foreach (var ws in RelevantWorkSets(workSet))
            {
                if (!_middlewares.ContainsKey(ws))
                {
                    Debug.Fail($"工作集 '{ws}' 不存在，无法删除中间件");
                    throw new Exception($"工作集 '{ws}' 不存在，无法删除中间件");
                }

                var index = _middlewares[ws].FindIndex(m => m.ID == id);
                if (index == -1)
                {
                    continue;
                }

                _middlewares[ws].RemoveAt(index);
                deleted++;
            }

            if (deleted == 0)
            {
                Debug.Fail($"没有 ID 为 '{id}' 的中间件可供删除");
                throw new Exception($"没有 ID 为 '{id}' 的中间件可供删除");
            }
        }

        /// <summary>
        /// 删除所有中间件
        /// </summary>
        public void ClearAllMiddlewares()
        {
            _middlewares = new Dictionary<string, List<Middleware>>();
        }

        // 对各 Work Set 中的中间件依据 Priority 进行排序
        private void PrepareMiddlewares()
        {
            foreach (var ws in _middlewares.Keys.ToList())
            {
                _middlewares[ws].Sort((m1, m2) =>
                {
                    if (m1 == m2)
                    {
                        return 0;
                    }
                    return m1.Priority > m2.Priority ? -1 : 1;
                });
            }
        }

        #endregion

        #region 中间件调用

        public void Setup()
        {
            PrepareMiddlewares();

            foreach (var ws in _middlewares.Keys.ToList())
            {
                foreach (var m in _middlewares[ws])
                {
                    m.Setup();
                }
            }
        }

        public void Teardown()
        {
            foreach (var ws in _middlewares.Keys.ToList())
            {
                foreach (var m in _middlewares[ws])
                {
                    m.TearDown();
                }
            }
        }

        public void Run(object requset)
        {
            var contexts = new Dictionary<string, Context>();

            var tasks = new List<Task>();
            foreach (var ws in _middlewares.Keys.ToList())
            {
                if (_middlewares[ws].Count != 0)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        contexts.Add(ws, new Context { Request = requset });
                        DoBeforeOnWorkSet(ws, contexts[ws]);
                    }));
                }
            }
            Task.WaitAll(tasks.ToArray());

            tasks.Clear();
            foreach (var ws in _middlewares.Keys.ToList())
            {
                if (_middlewares[ws].Count != 0)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        DoAfterOnWorkSet(ws, contexts[ws]);
                    }));
                }
            }
            Task.WaitAll(tasks.ToArray());
        }



        private void DoBeforeOnWorkSet(string workSet, Context context)
        {
            foreach (var m in _middlewares[workSet])
            {
                m.Before(context);
            }
        }

        private void DoAfterOnWorkSet(string workSet, Context context)
        {
            foreach (var m in _middlewares[workSet])
            {
                m.After(context);
            }
        }

        #endregion

    }
}
