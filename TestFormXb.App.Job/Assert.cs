using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestFormXb
{
    public class Assert
    {
        public static bool ThrowExceptionOnFailed { get; set; } = true;

        private static TextBox _textBox;
        private static int _uiThreadId = -1;
        private static TaskScheduler _uiTaskScheduler;
        private static Dictionary<string, int> _assertCountList = new Dictionary<string, int>();

        public static void Init(TextBox textBox)
        {
            Assert._textBox = textBox;
            Assert._uiThreadId = Thread.CurrentThread.ManagedThreadId;
            Assert._uiTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }




        private static void WriteResult(bool result, string testName)
        {
            if (Assert._assertCountList.ContainsKey(testName))
                Assert._assertCountList[testName]++;
            else
                Assert._assertCountList.Add(testName, 1);

            var assertCount = Assert._assertCountList[testName];
            var msg = $"{testName.PadRight(15)}: {assertCount.ToString().PadLeft(2)} - {(result ? " Ok." : "*** FAILURE!! **")}";

            var action = new Action(() =>
            {
                var currentMsg = Assert._textBox.Text;
                Assert._textBox.Text = $@"{currentMsg}{(string.IsNullOrEmpty(currentMsg) ? "" : "\r\n")}{msg}";
                Assert._textBox.Refresh();
                Assert._textBox.SelectionStart = Assert._textBox.Text.Length;
                Assert._textBox.Focus();
                Assert._textBox.ScrollToCaret();


                if (Assert.ThrowExceptionOnFailed && !result)
                    throw new Exception(msg);
            });

            if (Assert._uiThreadId == Thread.CurrentThread.ManagedThreadId)
            {
                action.Invoke();
            }
            else
            {
                var task = new Task(action);
                task.Start(Assert._uiTaskScheduler);
            }
        }



        public static void IsTrue(bool value, string name = null)
        {
            //StackFrame frame = new StackFrame(1);
            //var method = frame.GetMethod();
            //var type = method.DeclaringType;
            //var name = method.Name;
            var methodName = name ?? ((new StackFrame(1)).GetMethod()).Name;

            Assert.WriteResult((value == true), methodName);
        }

        public static void IsFalse(bool value, string name = null)
        {
            var methodName = name ?? ((new StackFrame(1)).GetMethod()).Name;
            Assert.WriteResult((value != true), methodName);
        }

        public static void IsNull(object value, string name = null)
        {
            var methodName = name ?? ((new StackFrame(1)).GetMethod()).Name;
            Assert.WriteResult((value == null), methodName);
        }

        public static void IsNotNull(object value, string name = null)
        {
            var methodName = name ?? ((new StackFrame(1)).GetMethod()).Name;
            Assert.WriteResult((value != null), methodName);
        }

        public static void AreEqual(object value1, object value2, string name = null)
        {
            var methodName = name ?? ((new StackFrame(1)).GetMethod()).Name;

            if (value1 == null
                || value2 == null)
            {
                Assert.WriteResult((value1 == null && value2 == null), methodName);
            }
            else
            {
                Assert.WriteResult((value1.Equals(value2)), methodName);
            }
        }

        public static void AreNotEqual(object value1, object value2, string name = null)
        {
            var methodName = name ?? ((new StackFrame(1)).GetMethod()).Name;

            if (value1 == null
                  || value2 == null)
            {
                Assert.WriteResult(!(value1 == null && value2 == null), methodName);
            }
            else
            {
                Assert.WriteResult(!(value1.Equals(value2)), methodName);
            }
        }

        public static void IsTimeOver(DateTime startTime, int mSec, string name = null)
        {
            var methodName = name ?? ((new StackFrame(1)).GetMethod()).Name;

            Assert.WriteResult(((DateTime.Now - startTime).TotalMilliseconds > mSec), methodName);
        }
    }
}
