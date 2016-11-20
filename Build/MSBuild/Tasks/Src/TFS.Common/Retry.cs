//---------------------------------------------------------------------------------------------------------
// <copyright file="Tfs.cs" holder="Jeff Jin Hung Tong">
//     Copyright (c) Jeff Tong.  All rights reserved.
// </copyright>
// <summary>
//      Retry static class.
// </summary>
//---------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;

namespace Twine.MSBuild.Tasks.TFS.Common
{
    public static class Retry
    {

        /// <summary>
        /// Will retry the passed in action.
        /// </summary>
        /// <param name="action"><see cref="Action"/>Method with no return type.</param>
        /// <param name="sleepTime"><see cref="TimeSpan"/>The time to wait before attempting again.</param>
        /// <param name="retryCount"><see cref="int"/>Number of times to retry.</param>
        public static void Do(Action action, TimeSpan sleepTime, int retryCount = 3)
        {
            Do<object>(() =>
            {
                action();
                return null;
            }, sleepTime, retryCount);
        }


        /// <summary>
        /// Will retry the passed in method with return value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"><see cref="Func{Object[], T}"/>Method with return type.</param>
        /// <param name="sleepTime"><see cref="TimeSpan"/>The time to wait before attempting again.</param>
        /// <param name="retryCount"><see cref="int"/>Number of times to retry.</param>
        /// <returns><see cref="T"/></returns>
        public static T Do<T>(Func<T> action, TimeSpan sleepTime, int retryCount = 3)
        {
            var exceptions = new List<Exception>();

            for (int retry = 0; retry < retryCount; retry++)
            {
                try
                {
                    if (retry > 0)
                    {
                        Thread.Sleep(sleepTime);
                    }

                    return action();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            throw new AggregateException(exceptions);
        }


        /// <summary>
        /// Will retry the passed in method with return value and parameters.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"><see cref="Func{Object[], T}"/>Method with return type.</param>
        /// <param name="sleepTime"><see cref="TimeSpan"/>The time to wait before attempting again.</param>
        /// <param name="retryCount"><see cref="int"/>Number of times to retry.</param>
        /// <param name="args">Input arguments for the passed in method.</param>
        /// <returns><see cref="T"/></returns>
        public static T Do<T>(Func<object[], T> action, TimeSpan sleepTime, int retryCount, params object[] args)
        {
            var exceptions = new List<Exception>();

            for (int retry = 0; retry < retryCount; retry++)
            {
                try
                {
                    if (retry > 0)
                    {
                        Thread.Sleep(sleepTime);
                    }

                    return action(args);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            throw new AggregateException(exceptions);
        }

    }
}
