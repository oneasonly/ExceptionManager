using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using System.Windows;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks.Schedulers;
using System.Reflection;
using System.Security;
using System.Runtime.Serialization;

namespace ExceptionManager
{
    public static class Ex
    {
        #region const
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly string n = Environment.NewLine;
        private static readonly string defaultFunc = "${callsite:cleanNamesOfAnonymousDelegates=true:cleanNamesOfAsyncContinuations=true}";
        public static readonly TaskFactory LongTask = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
        private static readonly TaskScheduler IOTask = new IOTaskScheduler();
        public static readonly Task TaskEmpty = Task.Run(() => { });
        #endregion

        #region props
        public static bool isAutonomMode { get; set; } = false;
        #endregion

        #region Public Methods
        public static Task LongSyncRun(Action func)
        {
            return Task.Factory.StartNew(func
                //, new CancellationToken()
                , TaskCreationOptions.LongRunning
                //, IOTask
                );
        }
        public static Task<T> LongSyncRun<T>(Func<T> func)
        {
            return Task.Factory.StartNew(func
                , new CancellationToken()
                , TaskCreationOptions.LongRunning
                , IOTask
                );
        }
        public static async Task RunUIAwait(TaskScheduler context, Action func)
        {
            await Task.Factory.StartNew(func, new CancellationToken(), TaskCreationOptions.None, context);
        }
        public static Task RunUI(TaskScheduler context, Action func)
        {
            return Task.Factory.StartNew(func, new CancellationToken(), TaskCreationOptions.None, context);
        }
        public static bool Try(Action func)
        {
            return Try(true, func);
        }
        public static bool Try(bool isLog, Action func)
        {
            try
            {
                func();
                return true;
            }
            catch (Exception ex)
            {
                if (isLog) 
                { 
                    logger.Trace(ex, ex.Message);
                    Trace.WriteLine($"{ex.Message}");
                }
                return false;
            }
        }
        public static bool Try(Action tryFunc, Action<Exception> catchFunc)
        {
            try
            {
                tryFunc();
                return true;
            }
            catch (Exception ex)
            {
                catchFunc(ex);
                return false;
            }
        }
        public static async Task<bool> TryAsync(Action func, string msg = null)
        {
            Task task = Task.Run(() => func);
            return await Try(task);
        }
        public static async Task<bool> Try(Task func, string msg = null)
        {
            try
            {
                await func;
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static bool TryLog(Action func)
        {
            return TryLog(null, func);
        }
        public static bool TryLog(string msg, Action func)
        {
            try
            {
                func();
                return true;
            }
            catch (Exception ex)
            {
                ex.Log();
                //logger.ErrorFunc(func, ex, msg);
                return false;
            }
        }
        public static void Log(this Exception ex, string msg = null)
        {
            string result = ex.Message.prefix(msg);
            logger.ErrorStack(ex, result);
        }

        public static void Log(string msg)
        {
            logger.Trace(msg);
            Trace.WriteLine(msg);
        }
        public static bool Catch(Action func, string msg = null)
        {
            try
            {
                func();
                return true;
            }
            catch (Exception ex)
            {
                ex.Show(msg);
                return false;
            }
        }
        public static bool Catch(string msg, Action func)
        {
            return Catch(func, msg);
        }
        public static async Task<bool> Catch(Task func, string msg = null)
        {
            try
            {
                await func;
                return true;
            }
            catch (Exception ex)
            {
                ex.Show(msg);
                return false;
            }
        }
        public static async Task<T> Catch<T>(Task<T> func, T ifError = default(T))
        {
            try
            {
                return await func;
            }
            catch (Exception ex)
            {
                ex.Show("Critical ERROR !!! CatchTask");
                return ifError;
            }
        }
        public static async Task<bool> CatchAsync(Action func, string msg = null)
        {
            Task task = Task.Run(() => func);
            return await Catch(task);
        }
        public static void Throw(Action func)
        {
            Throw(null, func);
        }
        public static void Throw(string msg, Action func)
        {
            try
            {
                func();
            }
            catch (Exception ex)
            {
                ex.Throw(msg);
            }
        }
        public static void Throw(string msg)
        {
            throw new CustException(msg);
        }

        public static void Throw(this Exception ex, string msg=null)
        {
            msg = $"throw {ex.Message.prefix(msg)}";
            //logger.ErrorStack(ex, msg);
            throw new Exception(msg, ex);
        }
        public static void Show(this Exception ex, string msg = null)
        {
            string resultMsg = msg;
            resultMsg = ex.Message.prefix(msg, 2);
            logger.ErrorStack(ex, resultMsg);
            Show(resultMsg);
        }
        public static void Show(this Exception ex, Action func)
        {
            //logger.ErrorFunc(func, ex);
            ex.Log();
            Show(ex.Message);
        }
        public static void Show(this Exception ex, Action<Exception> func, string msg = null)
        {
            string result = ex.Message.prefix(msg);
            func(ex);
            Show(result);
        }
        public static void Show(string msg, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Error)
        {
            if (isAutonomMode)
            {
                LongSyncRun(() => MessageBox.Show(msg.space(), "", buttons, icon));
            }
            else //false isAutonomMode
            {
                MessageBox.Show(msg.space(), "", buttons, icon);
            }
        }
        public static string Info(this Exception ex)
        {
            return $"{ex.Message}{n}{n}{ex.GetType().FullName}:{n}{ex.ReversedStackTrace()}";
        }
        public static void timeout(Action func)
        {
            //var task = Task.Run(func);
            var task = Task.Factory.StartNew(func);
            if (task.Wait(TimeSpan.FromSeconds(2)))
            { }
            else
            {
                throw new TimeoutException("Timed out");
            }
        }
        public static T CreateCopy<T>(T aobject)
        {
            ICloneable cl = (aobject as ICloneable);
            if (null != cl)
                return (T)cl.Clone();
            MethodInfo memberwiseClone = aobject?.GetType().GetMethod("MemberwiseClone",
                            BindingFlags.Instance | BindingFlags.NonPublic);
            T Copy = (T)memberwiseClone?.Invoke(aobject, null);
            foreach (FieldInfo f in typeof(T).GetFields(
                            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                object original = f.GetValue(aobject);
                f.SetValue(Copy, CreateCopy(original));
            }
            return Copy;
        }
        #endregion

        #region private Methods
        public static Exception InnerGetLast(this Exception ex)
        {
            while (ex?.InnerException != null)
            {
                ex = ex?.InnerException ?? ex;
            }
            return ex;
        }
        private static void ErrorFunc(this Logger thisLogger, Action func, Exception ex, string msg = null)
        {
            try
            {
                string methodName = $"{func.Method.DeclaringType.FullName}.{func.Method.Name}";
                methodName = methodName.Replace('<', '(').Replace('>', ')');
                if (LogManager.Configuration == null)
                {
                    logger.Error(ex, msg ?? ex.Message);
                    return;
                }
                LogManager.Configuration.Variables["func"] = methodName;
                LogManager.Configuration.Variables["myStackTrace"] = StackTraceNoSystem(Environment.StackTrace);
                logger.Error(ex, msg ?? ex.Message);
                Debug.WriteLine(msg ?? ex.Message);
                LogManager.Configuration.Variables["func"] = defaultFunc;
                LogManager.Configuration.Variables["myStackTrace"] = string.Empty;
            }
            catch (Exception ex2)
            {
                ex2.Log("Ошибка в Ex.Error()");
            }
        }
        private static void ErrorStack(this Logger thisLogger, Exception ex, string msg = null)
        {
            try
            {
                var cleanStackTrace = StackTraceNoSystem(ex.StackTraceInner());
                cleanStackTrace = StackTraceNoEx(cleanStackTrace);
                if (LogManager.Configuration == null)
                {
                    logger.Error(ex, msg ?? ex.Message);
                    return;
                }
                LogManager.Configuration.Variables["func"] = GetFirstLine(cleanStackTrace);
                LogManager.Configuration.Variables["myStackTrace"] = cleanStackTrace;
                logger.Error(ex, (msg ?? ex.Message).prefix(cleanStackTrace));
                Debug.WriteLine((msg ?? ex.Message).prefix(cleanStackTrace));                
                LogManager.Configuration.Variables["func"] = defaultFunc;
                LogManager.Configuration.Variables["myStackTrace"] = string.Empty;
            }
            catch (Exception ex2)
            {
                string str = "Ошибка в Ex.ErrorStack()";
                Debug.WriteLine($"{str} {ex2.Message}");
                logger.Error(ex2, str);
            }
        }

        private static string ReversedStackTrace(this Exception ex)
        {
            return StackTraceNoSystem(ex.StackTraceInner(), true);
        }
        private static string StackTraceInner(this Exception ex)
        {
            return ex?.InnerException?.StackTraceInner() ?? ex.StackTrace ?? string.Empty;
        }
        private static string StackTraceNoSystem(string text, bool isReverse = false)
        {
            if (string.IsNullOrEmpty(text))
            { return string.Empty; }
            var parts = text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder result = new StringBuilder();
            var regex = new Regex(@"\bSystem.");
            var partsToCheck = isReverse ? parts.Reverse() : parts;
            var prevLine = "";
            bool firstmatch = false;
            foreach (var tab in partsToCheck)
            {
                string line = tab.Trim().Trim('\r');
                if (!regex.IsMatch(line))
                {
                    if (firstmatch == false)
                    { result.AppendLine($" {prevLine}"); }
                    result.AppendLine($" {line}");
                    firstmatch = true;
                }
                prevLine = line;
            }
            return result.ToString();
        }
        private static string StackTraceNoEx(string text)
        {
            if (string.IsNullOrEmpty(text))
            { return string.Empty; }
            var parts = text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder result = new StringBuilder();
            var regex = new Regex(@"ExceptionManager.Ex");
            var partsToCheck = parts;
            foreach (var tab in partsToCheck)
            {
                string line = tab.Trim().Trim('\r');
                if (!regex.IsMatch(line))
                {
                    result.AppendLine($" {line}");
                }
            }
            return result.ToString();
        }
        private static string GetFirstLine(string text)
        {
            if (string.IsNullOrEmpty(text))
            { return string.Empty; }
            string result = string.Empty;
            //string[] exclude = { "Server stack trace", "\bSystem.", "\bEx." };
            var exclude = new[]
            { new Regex(@"Server stack trace")
        , new Regex(@"\bSystem.")
        , new Regex(@"\bExceptionManager.Ex")
        };
            try
            {
                var parts = text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var regex = new Regex(@"[^\s(]{3,}");
                for (int i = 0; i < parts.Length; i++)
                {
                    if (regex.IsMatch(parts[i]))
                    {
                        bool isFoundExclusion = false;
                        for (int j = 0; j < exclude.Length; j++)
                        {
                            if (exclude[j].IsMatch(parts[i]))
                            {
                                isFoundExclusion = true;
                                break;
                            }
                        }
                        if (!isFoundExclusion)
                        {
                            result = regex.Match(parts?[i]).Value;
                            result = result.Replace('<', '(');
                            result = result.Replace('>', ')');
                            result = Regex.Replace(result, @"[:?*]", "");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            { logger.Error(ex, ex.Message); }
            return result;
        }
        #endregion

        #region txt extensions
        public static string prefix(this String mainMsg, string prefixMsg, int countSeparators = 1)
        {
            string separator = "";
            for (int i = 0; i < countSeparators; i++)
            {
                separator += n;
            }
            if (separator == "")
                separator = " ";
            return (string.IsNullOrEmpty(prefixMsg)) ? mainMsg : prefixMsg + separator + mainMsg;
        }
        public static void RunParallel(this Task task) { }
        public static string trySubstring(this String inc, int startIndex, int length)
        {
            try
            {
                return inc.Substring(startIndex, length);
            }
            catch
            { return inc; }
        }
        public static string trySubstring(this String inc, int length)
        {
            return trySubstring(inc, 0, length);
        }
        public static string space(this String inc, int length = 45)
        {
            //length=45 for textbox
            string result = inc;
            Regex regex = new Regex($@"[^\s]{{{length},}}");
            while (regex.IsMatch(result))
            {
                Match matchReg = regex.Match(result);
                int sizeFound = matchReg.Length;
                var index = matchReg.Index;
                result = result.Insert(index + length - 1, " ");
            }
            return result;
        }
        #endregion
    }
}
