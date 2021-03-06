/*
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

using log4net;
using MindTouch.Aws;
using MindTouch.Dream.Test;
using MindTouch.Dream.Test.Aws;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Dream.Storage.Test {
    [TestFixture]
    public class AwsS3WireupTests {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private DreamServiceInfo _storage;
        private DreamHostInfo _hostInfo;
        private AwsS3ClientConfig _config;

        [TestFixtureSetUp]
        public void Init() {
            _config = new AwsS3ClientConfig {
                Endpoint = AwsTestHelpers.AWS,
                Bucket = "bucket",
                RootPath = "root/path",
                PrivateKey = "private",
                PublicKey = "public",
            };
            _hostInfo = DreamTestHelper.CreateRandomPortHost();
        }

        [SetUp]
        public void Setup() {
            CreateStorageService();
            MockPlug.DeregisterAll();
        }

        [TearDown]
        public void Teardown() {
            _storage.WithPrivateKey().AtLocalHost.Delete();
        }

        [TestFixtureTearDown]
        public void GlobalCleanup() {
            _hostInfo.Dispose();
        }

        [Test]
        public void Can_init_and_read_file() {
            var data = AwsTestHelpers.CreateRandomDocument();
            MockPlug.Setup(AwsTestHelpers.AWS.S3Uri)
                .Verb("GET")
                .At(_config.RootedPath("foo", "bar"))
                .Returns(DreamMessage.Ok(data))
                .ExpectAtLeastOneCall();
            var response = _storage.AtLocalHost.At("foo", "bar").Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(data.ToCompactString(), response.ToDocument().ToCompactString());
        }

        private void CreateStorageService() {
            var config = new XDoc("config")
                    .Elem("endpoint", _config.Endpoint.Name)
                    .Elem("folder", _config.RootPath)
                    .Elem("bucket", _config.Bucket)
                    .Elem("privatekey", _config.PrivateKey)
                    .Elem("publickey", _config.PublicKey);
            _storage = DreamTestHelper.CreateService(_hostInfo, "sid://mindtouch.com/2010/10/dream/s3.storage", "store", config);
        }
    }
}