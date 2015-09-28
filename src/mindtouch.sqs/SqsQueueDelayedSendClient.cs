﻿/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2014 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using log4net;

namespace MindTouch.Sqs {

    /// <summary>
    /// Class for enqueuing messages asynchronously for batched delivery.
    /// </summary>
    public sealed class SqsQueueDelayedSendClient {

        //--- Constants ---
        private static readonly TimeSpan AUTOFLUSH_TIME = 250.Milliseconds();
        
        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---

        /// <summary>
        /// Queue name.
        /// </summary>
        public readonly SqsQueueName QueueName;

        private readonly ISqsClient _client;
        private readonly TimedAccumulator<string> _timedSendAccumulator;

        //--- Constructors ---

        /// <summary>
        /// Constructor for creating an instance.
        /// </summary>
        /// <param name="client">ISqsClient instance.</param>
        /// <param name="queueName">Queue name.</param>
        /// <param name="timerFactory">TimerFactory instance.</param>
        public SqsQueueDelayedSendClient(ISqsClient client, SqsQueueName queueName, TaskTimerFactory timerFactory) {
            if(client == null) {
                throw new ArgumentNullException("client");
            }
            this.QueueName = queueName;
            _client = client;
            _timedSendAccumulator = new TimedAccumulator<string>(items => AsyncUtil.ForkBackgroundSender(() => BatchSendMessages(items)), SqsUtils.MAX_NUMBER_OF_BATCH_SEND_MESSAGES, AUTOFLUSH_TIME, timerFactory);
        }

        //--- Methods ---

        /// <summary>
        /// Enqueue message for batched, asynchronous delivery.
        /// </summary>
        /// <param name="messageBody">Message body.</param>
        public void EnqueueMessage(string messageBody) {
            _timedSendAccumulator.Enqueue(messageBody);
        }

        private void BatchSendMessages(IEnumerable<string> items) {
            _client.SendMessages(QueueName, items);
        }
    }
}