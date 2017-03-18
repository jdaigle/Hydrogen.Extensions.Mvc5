using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Async;
using System.Web.Mvc.Filters;
using Moq;
using Xunit;

namespace MvcAsync
{
    /*
     * These tests are essentially copied from "System.Web.Mvc.Async.Test.AsyncControllerActionInvokerTest, System.Web.Mvc.Test"
     * with a few changes to support the changed implementation.
     */
    public class AsyncControllerActionInvokerTest
    {
        [Fact]
        public void InvokeAction_ActionNotFound()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            // Act
            IAsyncResult asyncResult = invoker.BeginInvokeAction(controllerContext, "ActionNotFound", null, null);
            bool retVal = invoker.EndInvokeAction(asyncResult);

            // Assert
            Assert.False(retVal);
        }

        [Fact]
        public void InvokeAction_ActionThrowsException_Handled()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            // Act & assert
            IAsyncResult asyncResult = invoker.BeginInvokeAction(controllerContext, "ActionThrowsExceptionAndIsHandled", null, null);
            //TODO: Result filter may execute synchronously
            //Assert.Null(((TestController)controllerContext.Controller).Log); // Result filter shouldn't have executed yet

            bool retVal = invoker.EndInvokeAction(asyncResult);
            Assert.True(retVal);
            Assert.Equal("From exception filter", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_ActionThrowsException_NotHandled()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            // Act & assert
            AssertEx.Throws<Exception>(delegate
                {
                    var asyncResult = invoker.BeginInvokeAction(controllerContext, "ActionThrowsExceptionAndIsNotHandled", null, null);
                    invoker.EndInvokeAction(asyncResult);
                },
                @"Some exception text.");
        }

        [Fact]
        public void InvokeAction_ActionThrowsException_ThreadAbort()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            // Act & assert
            AssertEx.Throws<ThreadAbortException>(
                delegate
                {
                    var asyncResult = invoker.BeginInvokeAction(controllerContext, "ActionCallsThreadAbort", null, null);
                    invoker.EndInvokeAction(asyncResult);
                });
        }

        [Fact]
        public void InvokeAction_AuthenticationFilterShortCircuits()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            // Act
            IAsyncResult asyncResult = invoker.BeginInvokeAction(controllerContext, "AuthenticationFilterShortCircuits", null, null);
            bool retVal = invoker.EndInvokeAction(asyncResult);

            // Assert
            Assert.True(retVal);
            Assert.Equal("From authentication filter", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_AuthenticationFilterChallenges()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            // Act
            IAsyncResult asyncResult = invoker.BeginInvokeAction(controllerContext, "AuthenticationFilterChallenges", null, null);
            bool retVal = invoker.EndInvokeAction(asyncResult);

            // Assert
            Assert.True(retVal);
            Assert.Equal("From authentication filter challenge", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_AuthenticationFilterShortCircuitsAndChallenges()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            // Act
            IAsyncResult asyncResult = invoker.BeginInvokeAction(controllerContext, "AuthenticationFilterShortCircuitsAndChallenges", null, null);
            bool retVal = invoker.EndInvokeAction(asyncResult);

            // Assert
            Assert.True(retVal);
            Assert.Equal("From authentication filter challenge", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_AuthorizationFilterShortCircuits()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            // Act
            IAsyncResult asyncResult = invoker.BeginInvokeAction(controllerContext, "AuthorizationFilterShortCircuits", null, null);
            bool retVal = invoker.EndInvokeAction(asyncResult);

            // Assert
            Assert.True(retVal);
            Assert.Equal("From authorization filter", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_AuthorizationFilterShortCircuitsAndChallenges()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            // Act
            IAsyncResult asyncResult = invoker.BeginInvokeAction(controllerContext, "AuthorizationFilterShortCircuitsAndChallenges", null, null);
            bool retVal = invoker.EndInvokeAction(asyncResult);

            // Assert
            Assert.True(retVal);
            Assert.Equal("From authentication filter challenge", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_NormalAction()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            // Act
            IAsyncResult asyncResult = invoker.BeginInvokeAction(controllerContext, "NormalAction", null, null);
            bool retVal = invoker.EndInvokeAction(asyncResult);

            // Assert
            Assert.True(retVal);
            Assert.Equal("From action", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_OverrideFindAction()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerWithCustomFindAction();

            // Act
            IAsyncResult asyncResult = invoker.BeginInvokeAction(controllerContext, actionName: "Non-ExistantAction", callback: null, state: null);
            bool retVal = invoker.EndInvokeAction(asyncResult);

            // Assert
            Assert.True(retVal);
            Assert.Equal("From action", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_RequestValidationFails()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext(passesRequestValidation: false);
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            // Act & assert
            AssertEx.Throws<HttpRequestValidationException>(
                delegate { invoker.BeginInvokeAction(controllerContext, "NormalAction", null, null); });
        }

        [Fact]
        public void InvokeAction_ResultThrowsException_Handled()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            // Act & assert
            IAsyncResult asyncResult = invoker.BeginInvokeAction(controllerContext, "ResultThrowsExceptionAndIsHandled", null, null);
            bool retVal = invoker.EndInvokeAction(asyncResult);

            Assert.True(retVal);
            Assert.Equal("From exception filter", ((TestController)controllerContext.Controller).Log);
        }

        [Fact]
        public void InvokeAction_ResultThrowsException_NotHandled()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            // Act & assert
            IAsyncResult asyncResult = invoker.BeginInvokeAction(controllerContext, "ResultThrowsExceptionAndIsNotHandled", null, null);
            AssertEx.Throws<Exception>(delegate { invoker.EndInvokeAction(asyncResult); },
                @"Some exception text.");
        }

        [Fact]
        public void InvokeAction_ResultThrowsException_ThreadAbort()
        {
            // Arrange
            ControllerContext controllerContext = GetControllerContext();
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            // Act & assert
            IAsyncResult asyncResult = invoker.BeginInvokeAction(controllerContext, "ResultCallsThreadAbort", null, null);
            AssertEx.Throws<ThreadAbortException>(delegate { invoker.EndInvokeAction(asyncResult); });
        }

        [Fact]
        public void InvokeAction_ThrowsIfActionNameIsEmpty()
        {
            // Arrange
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            // Act & assert
            AssertEx.ThrowsArgumentNullOrEmpty(
                delegate { invoker.BeginInvokeAction(new ControllerContext(), "", null, null); }, "actionName");
        }

        [Fact]
        public void InvokeAction_ThrowsIfActionNameIsNull()
        {
            // Arrange
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            // Act & assert
            AssertEx.ThrowsArgumentNullOrEmpty(
                delegate { invoker.BeginInvokeAction(new ControllerContext(), null, null, null); }, "actionName");
        }

        [Fact]
        public void InvokeAction_ThrowsIfControllerContextIsNull()
        {
            // Arrange
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            // Act & assert
            AssertEx.ThrowsArgumentNull(
                delegate { invoker.BeginInvokeAction(null, "someAction", null, null); }, "controllerContext");
        }

        [Fact]
        public void InvokeActionMethod_AsynchronousDescriptor()
        {
            // Arrange
            ControllerContext controllerContext = new ControllerContext();
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            IAsyncResult innerAsyncResult = new MockAsyncResult();
            ActionResult expectedResult = new ViewResult();

            AsyncCallback callback = null;
            Mock<AsyncActionDescriptor> mockActionDescriptor = new Mock<AsyncActionDescriptor>();
            mockActionDescriptor.Setup(d => d.BeginExecute(controllerContext, parameters, It.IsAny<AsyncCallback>(), It.IsAny<object>()))
                .Callback<ControllerContext, IDictionary<string, object>, AsyncCallback, object>((a, b, _callback, state) =>
                {
                    callback = _callback;
                })
                .Returns(innerAsyncResult);
            mockActionDescriptor.Setup(d => d.EndExecute(innerAsyncResult)).Returns(expectedResult);

            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            // Act
            var task = invoker.InvokeActionMethodAsync(controllerContext, mockActionDescriptor.Object, parameters);
            callback(innerAsyncResult);
            ActionResult returnedResult = task.Result;
            //IAsyncResult asyncResult = invoker.BeginInvokeActionMethod(controllerContext, mockActionDescriptor.Object, parameters, null, null);
            //ActionResult returnedResult = invoker.EndInvokeActionMethod(asyncResult);

            // Assert
            Assert.Equal(expectedResult, returnedResult);
        }

        [Fact]
        public void InvokeActionMethod_SynchronousDescriptor()
        {
            // Arrange
            ControllerContext controllerContext = new ControllerContext();
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            ActionResult expectedResult = new ViewResult();

            Mock<ActionDescriptor> mockActionDescriptor = new Mock<ActionDescriptor>();
            mockActionDescriptor.Setup(d => d.Execute(controllerContext, parameters)).Returns(expectedResult);

            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();

            // Act
            ActionResult returnedResult = invoker.InvokeActionMethodAsync(controllerContext, mockActionDescriptor.Object, parameters).GetAwaiter().GetResult();
            //IAsyncResult asyncResult = invoker.BeginInvokeActionMethod(controllerContext, mockActionDescriptor.Object, parameters, null, null);
            //ActionResult returnedResult = invoker.EndInvokeActionMethod(asyncResult);

            // Assert
            Assert.Equal(expectedResult, returnedResult);
        }

        [Fact]
        public void BeginInvokeActionMethodWithFilters_BeginExecuteThrowsOnActionExecutingException_Handled()
        {
            // Arrange
            ActionResult expectedResult = new ViewResult();
            Exception expectedException = new Exception("Some exception text.");
            bool onActionExecutingWasCalled = false;
            bool onActionExecutedWasCalled = false;
            ActionFilterImpl actionFilter = new ActionFilterImpl()
            {
                OnActionExecutingImpl = filterContext => { onActionExecutingWasCalled = true; },
                OnActionExecutedImpl = filterContext =>
                {
                    onActionExecutedWasCalled = true;
                    Assert.Same(expectedException, filterContext.Exception);
                    filterContext.ExceptionHandled = true;
                    filterContext.Result = expectedResult;
                }
            };
            Func<IAsyncResult> beginExecute = delegate
            {
                throw expectedException;
            };

            // Act
            ActionResult result = BeingInvokeActionMethodWithFiltersBeginTester(beginExecute, actionFilter);

            // Assert
            Assert.True(onActionExecutingWasCalled);
            Assert.True(onActionExecutedWasCalled);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void BeginInvokeActionMethodWithFilters_BeginExecuteThrowsOnActionExecutingException_HandledByEarlier()
        {
            // Arrange
            ActionResult expectedResult = new ViewResult();
            List<string> actionLog = new List<string>();
            Exception exception = new Exception("Some exception text.");
            Func<IAsyncResult> beginExecute = delegate
            {
                actionLog.Add("BeginExecute");
                throw exception;
            };
            ActionFilterImpl filter1 = new ActionFilterImpl()
            {
                OnActionExecutingImpl = delegate (ActionExecutingContext filterContext) { actionLog.Add("OnActionExecuting1"); },
                OnActionExecutedImpl = delegate (ActionExecutedContext filterContext)
                {
                    actionLog.Add("OnActionExecuted1");
                    Assert.Same(exception, filterContext.Exception);
                    filterContext.ExceptionHandled = true;
                    filterContext.Result = expectedResult;
                }
            };
            ActionFilterImpl filter2 = new ActionFilterImpl()
            {
                OnActionExecutingImpl = delegate (ActionExecutingContext filterContext) { actionLog.Add("OnActionExecuting2"); },
                OnActionExecutedImpl = delegate (ActionExecutedContext filterContext) { actionLog.Add("OnActionExecuted2"); }
            };

            // Act
            ActionResult result = BeingInvokeActionMethodWithFiltersBeginTester(beginExecute, filter1, filter2);

            // Assert
            Assert.Equal(new[] { "OnActionExecuting1", "OnActionExecuting2", "BeginExecute", "OnActionExecuted2", "OnActionExecuted1" }, actionLog.ToArray());
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void BeginInvokeActionMethodWithFilters_BeginExecuteThrowsOnActionExecutingException_HandledByLater()
        {
            // Arrange
            List<string> actionLog = new List<string>();
            Exception exception = new Exception("Some exception text.");
            ActionResult expectedResult = new ViewResult();
            Func<IAsyncResult> beginExecute = delegate
            {
                actionLog.Add("BeginExecute");
                throw exception;
            };
            ActionFilterImpl filter1 = new ActionFilterImpl()
            {
                OnActionExecutingImpl = delegate (ActionExecutingContext filterContext) { actionLog.Add("OnActionExecuting1"); },
                OnActionExecutedImpl = delegate (ActionExecutedContext filterContext) { actionLog.Add("OnActionExecuted1"); }
            };
            ActionFilterImpl filter2 = new ActionFilterImpl()
            {
                OnActionExecutingImpl = delegate (ActionExecutingContext filterContext) { actionLog.Add("OnActionExecuting2"); },
                OnActionExecutedImpl = delegate (ActionExecutedContext filterContext)
                {
                    actionLog.Add("OnActionExecuted2");
                    Assert.Same(exception, filterContext.Exception);
                    filterContext.ExceptionHandled = true;
                    filterContext.Result = expectedResult;
                }
            };

            // Act
            ActionResult result = BeingInvokeActionMethodWithFiltersBeginTester(beginExecute, filter1, filter2);

            // Assert
            Assert.Equal(new[] { "OnActionExecuting1", "OnActionExecuting2", "BeginExecute", "OnActionExecuted2", "OnActionExecuted1" }, actionLog.ToArray());
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void BeginInvokeActionMethodWithFilters_BeginExecuteThrowsOnActionExecutingException_NotHandled()
        {
            // Arrange
            string expectedExceptionText = "Some exception text.";
            Exception expectedException = new Exception(expectedExceptionText);
            bool onActionExecutingWasCalled = false;
            bool onActionExecutedWasCalled = false;
            ActionFilterImpl actionFilter = new ActionFilterImpl()
            {
                OnActionExecutingImpl = filterContext => { onActionExecutingWasCalled = true; },
                OnActionExecutedImpl = filterContext => { onActionExecutedWasCalled = true; }
            };
            Func<IAsyncResult> beginExecute = delegate
            {
                throw expectedException;
            };

            // Act & assert
            AssertEx.Throws<Exception>(
                delegate
                {
                    BeingInvokeActionMethodWithFiltersBeginTester(beginExecute, actionFilter);
                },
                expectedExceptionText);

            // Assert
            Assert.True(onActionExecutingWasCalled);
            Assert.True(onActionExecutedWasCalled);
        }

        [Fact]
        public void BeginInvokeActionMethodWithFilters_BeginExecuteThrowsOnActionExecutingException_ThreadAbort()
        {
            // Arrange
            bool onActionExecutingWasCalled = false;
            bool onActionExecutedWasCalled = false;
            ActionFilterImpl actionFilter = new ActionFilterImpl()
            {
                OnActionExecutingImpl = filterContext => { onActionExecutingWasCalled = true; },
                OnActionExecutedImpl = filterContext =>
                {
                    onActionExecutedWasCalled = true;
                    Thread.ResetAbort();
                }
            };
            Func<IAsyncResult> beginExecute = delegate
            {
                Thread.CurrentThread.Abort();
                return null;
            };

            // Act & assert
            AssertEx.Throws<ThreadAbortException>(
                delegate
                {
                    BeingInvokeActionMethodWithFiltersBeginTester(beginExecute, actionFilter);
                });

            // Assert
            Assert.True(onActionExecutingWasCalled);
            Assert.True(onActionExecutedWasCalled);
        }

        [Fact]
        public void BeginInvokeActionMethodWithFilters_EndExecuteThrowsOnActionExecutedException_Handled()
        {
            // Arrange
            ViewResult expectedResult = new ViewResult();
            Exception exepctedException = new Exception("Some exception message.");
            bool actionCalled = false;
            bool onActionExecutedCalled = false;
            ActionFilterImpl actionFilter = new ActionFilterImpl()
            {
                OnActionExecutedImpl = (filterContext) =>
                {
                    onActionExecutedCalled = true;
                    Assert.Same(exepctedException, filterContext.Exception);
                    filterContext.ExceptionHandled = true;
                    filterContext.Result = expectedResult;
                }
            };
            Func<ActionResult> action = () =>
            {
                actionCalled = true;
                throw exepctedException;
            };

            // Act
            ActionResult result = BeingInvokeActionMethodWithFiltersEndTester(action, actionFilter);

            // Assert
            Assert.True(actionCalled);
            Assert.True(onActionExecutedCalled);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void BeginInvokeActionMethodWithFilters_EndExecuteThrowsOnActionExecutedException_HandledByEarlier()
        {
            // Arrange
            List<string> actionLog = new List<string>();
            Exception exception = new Exception("Some exception message.");
            ViewResult expectedResult = new ViewResult();
            Func<ActionResult> action = delegate
            {
                actionLog.Add("EndExecute");
                throw exception;

            };
            ActionFilterImpl filter1 = new ActionFilterImpl()
            {
                OnActionExecutingImpl = delegate (ActionExecutingContext filterContext) { actionLog.Add("OnActionExecuting1"); },
                OnActionExecutedImpl = delegate (ActionExecutedContext filterContext)
                {
                    actionLog.Add("OnActionExecuted1");
                    Assert.Same(exception, filterContext.Exception);
                    filterContext.ExceptionHandled = true;
                    filterContext.Result = expectedResult;
                }
            };
            ActionFilterImpl filter2 = new ActionFilterImpl()
            {
                OnActionExecutingImpl = delegate (ActionExecutingContext filterContext) { actionLog.Add("OnActionExecuting2"); },
                OnActionExecutedImpl = delegate (ActionExecutedContext filterContext) { actionLog.Add("OnActionExecuted2"); }
            };

            // Act
            ActionResult result = BeingInvokeActionMethodWithFiltersEndTester(action, filter1, filter2);

            // Assert
            Assert.Equal(new[] { "OnActionExecuting1", "OnActionExecuting2", "EndExecute", "OnActionExecuted2", "OnActionExecuted1" }, actionLog.ToArray());
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void BeginInvokeActionMethodWithFilters_EndExecuteThrowsOnActionExecutedException_HandledByLater()
        {
            // Arrange
            List<string> actionLog = new List<string>();
            Exception exception = new Exception("Some exception message.");
            ActionResult expectedResult = new ViewResult();
            Func<ActionResult> action = delegate
            {
                actionLog.Add("EndExecute");
                throw exception;

            };
            ActionFilterImpl filter1 = new ActionFilterImpl()
            {
                OnActionExecutingImpl = delegate (ActionExecutingContext filterContext) { actionLog.Add("OnActionExecuting1"); },
                OnActionExecutedImpl = delegate (ActionExecutedContext filterContext) { actionLog.Add("OnActionExecuted1"); }
            };
            ActionFilterImpl filter2 = new ActionFilterImpl()
            {
                OnActionExecutingImpl = delegate (ActionExecutingContext filterContext) { actionLog.Add("OnActionExecuting2"); },
                OnActionExecutedImpl = delegate (ActionExecutedContext filterContext)
                {
                    actionLog.Add("OnActionExecuted2");
                    Assert.Same(exception, filterContext.Exception);
                    filterContext.ExceptionHandled = true;
                    filterContext.Result = expectedResult;
                }
            };

            // Act
            ActionResult result = BeingInvokeActionMethodWithFiltersEndTester(action, filter1, filter2);

            // Assert
            Assert.Equal(new[] { "OnActionExecuting1", "OnActionExecuting2", "EndExecute", "OnActionExecuted2", "OnActionExecuted1" }, actionLog.ToArray());
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void BeginInvokeActionMethodWithFilters_EndExecuteThrowsOnActionExecutedException_NotHandled()
        {
            // Arrange
            bool onActionExecutedWasCalled = false;
            string expectedExceptionText = "Some exception text.";
            ActionFilterImpl actionFilter = new ActionFilterImpl()
            {
                OnActionExecutedImpl = filterContext => { onActionExecutedWasCalled = true; }
            };
            Func<ActionResult> action = delegate
            {
                throw new Exception(expectedExceptionText);
            };

            // Act & assert
            AssertEx.Throws<Exception>(
                () => { BeingInvokeActionMethodWithFiltersEndTester(action, actionFilter); },
                expectedExceptionText);

            // Assert
            Assert.True(onActionExecutedWasCalled);
        }

        [Fact]
        public void BeginInvokeActionMethodWithFilters_EndExecuteThrowsOnActionExecutedException_ThreadAbort()
        {
            // Arrange
            bool onActionExecutedWasCalled = false;
            ActionFilterImpl actionFilter = new ActionFilterImpl()
            {
                OnActionExecutedImpl = filterContext =>
                {
                    onActionExecutedWasCalled = true;
                    Thread.ResetAbort();
                }
            };
            Func<ActionResult> action = delegate
            {
                Thread.CurrentThread.Abort();
                return null;
            };

            // Act & assert
            AssertEx.Throws<ThreadAbortException>(
                delegate { BeingInvokeActionMethodWithFiltersEndTester(action, actionFilter); });

            // Assert
            Assert.True(onActionExecutedWasCalled);
        }

        [Fact]
        public void BeginInvokeActionMethodWithFilters_NormalExecutionNotCanceled()
        {
            // Arrange
            bool onActionExecutingWasCalled = false;
            bool onActionExecutedWasCalled = false;
            MockAsyncResult innerAsyncResult = new MockAsyncResult();
            ActionFilterImpl actionFilter = new ActionFilterImpl()
            {
                OnActionExecutingImpl = _ => { onActionExecutingWasCalled = true; },
                OnActionExecutedImpl = _ => { onActionExecutedWasCalled = true; }
            };
            Func<IAsyncResult> beginExecute = delegate
            {
                return innerAsyncResult;
            };

            // Act
            ActionResult result = BeingInvokeActionMethodWithFiltersBeginTester(beginExecute, actionFilter);

            // Assert
            Assert.True(onActionExecutingWasCalled);
            Assert.True(onActionExecutedWasCalled);
        }

        [Fact]
        public void BeginInvokeActionMethodWithFilters_OnActionExecutingSetsResult()
        {
            // Arrange
            ActionResult expectedResult = new ViewResult();
            ActionResult overriddenResult = new ViewResult();
            bool onActionExecutingWasCalled = false;
            bool onActionExecutedWasCalled = false;
            ActionFilterImpl actionFilter = new ActionFilterImpl()
            {
                OnActionExecutingImpl = filterContext =>
                {
                    onActionExecutingWasCalled = true;
                    filterContext.Result = expectedResult;
                },
                OnActionExecutedImpl = _ => { onActionExecutedWasCalled = true; }
            };
            Func<ActionResult> endExecute = delegate
            {
                return overriddenResult;
            };

            // Act
            ActionResult result = BeingInvokeActionMethodWithFiltersTester(() => new MockAsyncResult(), endExecute, checkBegin: false, checkEnd: false, filters: new IActionFilter[] { actionFilter });

            // Assert
            Assert.True(onActionExecutingWasCalled);
            Assert.False(onActionExecutedWasCalled);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void BeginInvokeActionMethodWithFilters_FiltersOrderedCorrectly()
        {
            // Arrange
            List<string> actionLog = new List<string>();
            ActionResult actionResult = new ViewResult();
            Func<ActionResult> continuation = delegate
            {
                actionLog.Add("Continuation");
                return actionResult;

            };
            ActionFilterImpl filter1 = new ActionFilterImpl()
            {
                OnActionExecutingImpl = delegate (ActionExecutingContext filterContext) { actionLog.Add("OnActionExecuting1"); },
                OnActionExecutedImpl = delegate (ActionExecutedContext filterContext) { actionLog.Add("OnActionExecuted1"); }
            };
            ActionFilterImpl filter2 = new ActionFilterImpl()
            {
                OnActionExecutingImpl = delegate (ActionExecutingContext filterContext) { actionLog.Add("OnActionExecuting2"); },
                OnActionExecutedImpl = delegate (ActionExecutedContext filterContext) { actionLog.Add("OnActionExecuted2"); }
            };

            // Act
            ActionResult result = BeingInvokeActionMethodWithFiltersEndTester(continuation, filter1, filter2);

            // Assert
            Assert.Equal(new[] { "OnActionExecuting1", "OnActionExecuting2", "Continuation", "OnActionExecuted2", "OnActionExecuted1" }, actionLog.ToArray());
            Assert.Equal(actionResult, result);
        }

        [Fact]
        public void BeginInvokeActionMethodWithFilters_ShortCircuited()
        {
            // Arrange
            List<string> actionLog = new List<string>();
            ActionResult shortCircuitResult = new ViewResult();
            ActionResult executeResult = new ViewResult();
            ActionFilterImpl filter1 = new ActionFilterImpl()
            {
                OnActionExecutingImpl = delegate (ActionExecutingContext filterContext) { actionLog.Add("OnActionExecuting1"); },
                OnActionExecutedImpl = delegate (ActionExecutedContext filterContext) { actionLog.Add("OnActionExecuted1"); }
            };
            ActionFilterImpl filter2 = new ActionFilterImpl()
            {
                OnActionExecutingImpl = delegate (ActionExecutingContext filterContext)
                {
                    actionLog.Add("OnActionExecuting2");
                    filterContext.Result = shortCircuitResult;
                },
                OnActionExecutedImpl = delegate (ActionExecutedContext filterContext) { actionLog.Add("OnActionExecuted2"); }
            };
            Func<ActionResult> endExecute = () =>
            {
                actionLog.Add("ExecuteCalled");
                return executeResult;
            };

            // Act
            ActionResult result = BeingInvokeActionMethodWithFiltersTester(() => new MockAsyncResult(), endExecute, checkBegin: false, checkEnd: false, filters: new IActionFilter[] { filter1, filter2 });

            // Assert
            Assert.Equal(new[] { "OnActionExecuting1", "OnActionExecuting2", "OnActionExecuted1" }, actionLog.ToArray());
            Assert.Equal(shortCircuitResult, result);
        }

        private ActionResult BeingInvokeActionMethodWithFiltersBeginTester(Func<IAsyncResult> beginFunction, params IActionFilter[] filters)
        {
            return BeingInvokeActionMethodWithFiltersTester(beginFunction, () => new Mock<ActionResult>().Object, checkBegin: true, checkEnd: false, filters: filters);
        }

        private ActionResult BeingInvokeActionMethodWithFiltersEndTester(Func<ActionResult> endFunction, params IActionFilter[] filters)
        {
            return BeingInvokeActionMethodWithFiltersTester(() => new MockAsyncResult(), endFunction, checkBegin: true, checkEnd: true, filters: filters);
        }

        private ActionResult BeingInvokeActionMethodWithFiltersTester(Func<IAsyncResult> beginFunction, Func<ActionResult> endFunction, bool checkBegin, bool checkEnd, IActionFilter[] filters)
        {
            AsyncControllerActionInvokerEx invoker = new AsyncControllerActionInvokerEx();
            ControllerContext controllerContext = new ControllerContext();
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            Mock<AsyncActionDescriptor> mockActionDescriptor = new Mock<AsyncActionDescriptor>();
            bool endExecuteCalled = false;
            bool beginExecuteCalled = false;
            Func<ActionResult> endExecute = () =>
            {
                endExecuteCalled = true;
                return endFunction();
            };
            IAsyncResult innerAsyncResult = null;
            Func<IAsyncResult> beingExecute = () =>
            {
                beginExecuteCalled = true;
                innerAsyncResult = beginFunction();
                return innerAsyncResult;
            };

            AsyncCallback callback = null;
            mockActionDescriptor
                .Setup(d => d.BeginExecute(controllerContext, parameters, It.IsAny<AsyncCallback>(), It.IsAny<object>()))
                .Callback<ControllerContext, IDictionary<string, object>, AsyncCallback, object>((a, b, _callback, state) =>
                {
                    callback = _callback;
                })
                .Returns(beingExecute);
            mockActionDescriptor.Setup(d => d.EndExecute(It.IsAny<IAsyncResult>())).Returns(endExecute);

            //IAsyncResult outerAsyncResult = null;
            Task<ActionExecutedContext> invokeTask = null;
            try
            {
                invokeTask = invoker.InvokeActionMethodWithFiltersAsync(controllerContext, filters, mockActionDescriptor.Object, parameters);
                //outerAsyncResult = invoker.BeginInvokeActionMethodWithFilters(controllerContext, filters, mockActionDescriptor.Object, parameters, null, null);
            }
            catch (Exception ex)
            {
                if (checkEnd)
                {
                    // Testing end, so not expecting exception thrown from begin
                    Assert.NotNull(ex);
                }
                else
                {
                    throw ex;
                }
            }

            //Assert.NotNull(outerAsyncResult);
            Assert.Equal(checkBegin, beginExecuteCalled);
            Assert.False(endExecuteCalled);

            if (innerAsyncResult != null)
            {
                callback(innerAsyncResult); // call to end async invocation
            }

            ActionExecutedContext postContext = invokeTask.Result;
            //ActionExecutedContext postContext = invoker.EndInvokeActionMethodWithFilters(outerAsyncResult);

            Assert.NotNull(postContext);
            if (checkEnd)
            {
                Assert.True(endExecuteCalled);
            }
            return postContext.Result;
        }

        private static ControllerContext GetControllerContext(bool passesRequestValidation = true)
        {
            Mock<HttpContextBase> mockHttpContext = new Mock<HttpContextBase>();
            if (passesRequestValidation)
            {
#pragma warning disable 618
                mockHttpContext.Setup(o => o.Request.ValidateInput()).AtMostOnce();
#pragma warning restore 618
            }
            else
            {
                mockHttpContext.Setup(o => o.Request.ValidateInput()).Throws(new HttpRequestValidationException());
            }

            return new ControllerContext()
            {
                Controller = new TestController(),
                HttpContext = mockHttpContext.Object
            };
        }

        private class ActionFilterImpl : IActionFilter, IResultFilter
        {
            public Action<ActionExecutingContext> OnActionExecutingImpl { get; set; }

            public void OnActionExecuting(ActionExecutingContext filterContext)
            {
                if (OnActionExecutingImpl != null)
                {
                    OnActionExecutingImpl(filterContext);
                }
            }

            public Action<ActionExecutedContext> OnActionExecutedImpl { get; set; }

            public void OnActionExecuted(ActionExecutedContext filterContext)
            {
                if (OnActionExecutedImpl != null)
                {
                    OnActionExecutedImpl(filterContext);
                }
            }

            public Action<ResultExecutingContext> OnResultExecutingImpl { get; set; }

            public void OnResultExecuting(ResultExecutingContext filterContext)
            {
                if (OnResultExecutingImpl != null)
                {
                    OnResultExecutingImpl(filterContext);
                }
            }

            public Action<ResultExecutedContext> OnResultExecutedImpl { get; set; }

            public void OnResultExecuted(ResultExecutedContext filterContext)
            {
                if (OnResultExecutedImpl != null)
                {
                    OnResultExecutedImpl(filterContext);
                }
            }
        }

        public class AsyncControllerActionInvokerWithCustomFindAction : AsyncControllerActionInvokerEx
        {
            protected override ActionDescriptor FindAction(ControllerContext controllerContext, ControllerDescriptor controllerDescriptor, string actionName)
            {
                return base.FindAction(controllerContext, controllerDescriptor, "NormalAction");
            }
        }

        [ResetThreadAbort]
        private class TestController : AsyncController
        {
            public string Log;

            public ActionResult ActionCallsThreadAbortAsync()
            {
                Thread.CurrentThread.Abort();
                return null;
            }

            public ActionResult ActionCallsThreadAbortCompleted()
            {
                return null;
            }

            public ActionResult ResultCallsThreadAbort()
            {
                return new ActionResultWhichCallsThreadAbort();
            }

            public ActionResult NormalAction()
            {
                return new LoggingActionResult("From action");
            }

            [AuthenticationFilterReturnsResult]
            public void AuthenticationFilterShortCircuits()
            {
            }

            [AuthenticationFilterChallengeSetsResult]
            public void AuthenticationFilterChallenges()
            {
            }

            [AuthenticationFilterReturnsResult]
            [AuthenticationFilterChallengeSetsResult]
            public void AuthenticationFilterShortCircuitsAndChallenges()
            {
            }

            [AuthorizationFilterReturnsResult]
            public void AuthorizationFilterShortCircuits()
            {
            }

            [AuthenticationFilterChallengeSetsResult]
            [AuthorizationFilterReturnsResult]
            public void AuthorizationFilterShortCircuitsAndChallenges()
            {
            }

            [CustomExceptionFilterHandlesError]
            public void ActionThrowsExceptionAndIsHandledAsync()
            {
                throw new Exception("Some exception text.");
            }

            public void ActionThrowsExceptionAndIsHandledCompleted()
            {
            }

            [CustomExceptionFilterDoesNotHandleError]
            public void ActionThrowsExceptionAndIsNotHandledAsync()
            {
                throw new Exception("Some exception text.");
            }

            public void ActionThrowsExceptionAndIsNotHandledCompleted()
            {
            }

            [CustomExceptionFilterHandlesError]
            public ActionResult ResultThrowsExceptionAndIsHandled()
            {
                return new ActionResultWhichThrowsException();
            }

            [CustomExceptionFilterDoesNotHandleError]
            public ActionResult ResultThrowsExceptionAndIsNotHandled()
            {
                return new ActionResultWhichThrowsException();
            }

            private class AuthenticationFilterChallengeSetsResultAttribute : FilterAttribute, IAuthenticationFilter
            {
                public void OnAuthentication(AuthenticationContext filterContext)
                {
                }

                public void OnAuthenticationChallenge(AuthenticationChallengeContext filterContext)
                {
                    filterContext.Result = new LoggingActionResult("From authentication filter challenge");
                }
            }

            private class AuthenticationFilterReturnsResultAttribute : FilterAttribute, IAuthenticationFilter
            {
                public void OnAuthentication(AuthenticationContext filterContext)
                {
                    filterContext.Result = new LoggingActionResult("From authentication filter");
                }

                public void OnAuthenticationChallenge(AuthenticationChallengeContext filterContext)
                {
                }
            }

            private class AuthorizationFilterReturnsResultAttribute : FilterAttribute, IAuthorizationFilter
            {
                public void OnAuthorization(AuthorizationContext filterContext)
                {
                    filterContext.Result = new LoggingActionResult("From authorization filter");
                }
            }

            private class CustomExceptionFilterDoesNotHandleErrorAttribute : FilterAttribute, IExceptionFilter
            {
                public void OnException(ExceptionContext filterContext)
                {
                }
            }

            private class CustomExceptionFilterHandlesErrorAttribute : FilterAttribute, IExceptionFilter
            {
                public void OnException(ExceptionContext filterContext)
                {
                    filterContext.ExceptionHandled = true;
                    filterContext.Result = new LoggingActionResult("From exception filter");
                }
            }

            private class ActionResultWhichCallsThreadAbort : ActionResult
            {
                public override void ExecuteResult(ControllerContext context)
                {
                    Thread.CurrentThread.Abort();
                }
            }

            private class ActionResultWhichThrowsException : ActionResult
            {
                public override void ExecuteResult(ControllerContext context)
                {
                    throw new Exception("Some exception text.");
                }
            }
        }

        private class ResetThreadAbortAttribute : ActionFilterAttribute
        {
            public override void OnActionExecuted(ActionExecutedContext filterContext)
            {
                try
                {
                    Thread.ResetAbort();
                }
                catch (ThreadStateException)
                {
                    // thread wasn't being aborted
                }
            }

            public override void OnResultExecuted(ResultExecutedContext filterContext)
            {
                try
                {
                    Thread.ResetAbort();
                }
                catch (ThreadStateException)
                {
                    // thread wasn't being aborted
                }
            }
        }

        private class LoggingActionResult : ActionResult
        {
            private readonly string _logText;

            public LoggingActionResult(string logText)
            {
                _logText = logText;
            }

            public override void ExecuteResult(ControllerContext context)
            {
                ((TestController)context.Controller).Log = _logText;
            }
        }
    }
}
