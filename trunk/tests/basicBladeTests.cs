﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using bladeDirectorWCF;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace tests
{
    [TestClass]
    public class basicBladeTests
    {
        public static readonly string WCFPath = Path.Combine(Properties.Settings.Default.repoRoot, "trunk\\bladeDirectorWCF\\bin\\Debug\\bladeDirectorWCF.exe");
        public static readonly Uri WebURI = new Uri(Properties.Settings.Default.webURL);

        [TestMethod]
        public void canInitWithBladesAndGetListBack()
        {
            using (bladeDirectorDebugServices svc = new bladeDirectorDebugServices(WCFPath, new[] { "1.1.1.1", "2.2.2.2", "3.3.3.3" }))
            {
                string[] foundIPs = svc.svc.getAllBladeIP();

                Assert.AreEqual(3, foundIPs.Length);
                Assert.IsTrue(foundIPs.Contains("1.1.1.1"));
                Assert.IsTrue(foundIPs.Contains("2.2.2.2"));
                Assert.IsTrue(foundIPs.Contains("3.3.3.3"));
            }
        }

        [TestMethod]
        public void canGetBladeSpec()
        {
            using (bladeDirectorDebugServices svc = new bladeDirectorDebugServices(WCFPath))
            {
                bladeSpec spec1Expected = svc.svcDebug.createBladeSpec("blade1ip", "blade1iscsiIP", "blade1ILOIP", 111, false, VMDeployStatus.notBeingDeployed, null, "idk", "box1", bladeLockType.lockAll, bladeLockType.lockAll);
                bladeSpec spec2Expected = svc.svcDebug.createBladeSpec("blade2ip", "blade2iscsiIP", "blade2ILOIP", 222, false, VMDeployStatus.notBeingDeployed, null, "idk", "box2", bladeLockType.lockAll, bladeLockType.lockAll);
                bladeSpec[] expected = new[] {spec1Expected, spec2Expected};

                svc.svcDebug.initWithBladesFromBladeSpec(expected, false, NASFaultInjectionPolicy.retunSuccessful);

                bladeSpec spec1Actual = svc.svc.getBladeByIP_withoutLocking("blade1ip");
                bladeSpec spec2Actual = svc.svc.getBladeByIP_withoutLocking("blade2ip");
                bladeSpec[] actual = new[] { spec1Actual, spec2Actual };

                for (int i = 0; i < 2; i++)
                {
                    Assert.AreEqual(expected[i].bladeIP, actual[i].bladeIP);
                    Assert.AreEqual(expected[i].ESXiPassword, actual[i].ESXiPassword);
                    Assert.AreEqual(expected[i].ESXiUsername, actual[i].ESXiUsername);
                    Assert.AreEqual(expected[i].currentlyBeingAVMServer, actual[i].currentlyBeingAVMServer);
                    Assert.AreEqual(expected[i].currentlyHavingBIOSDeployed, actual[i].currentlyHavingBIOSDeployed);
                    Assert.AreEqual(expected[i].iLOIP, actual[i].iLOIP);
                    Assert.AreEqual(expected[i].kernelDebugPort, actual[i].kernelDebugPort);
                    Assert.AreEqual(expected[i].iLoPassword, actual[i].iLoPassword);
                    Assert.AreEqual(expected[i].iLoUsername, actual[i].iLoUsername);
                    Assert.AreEqual(expected[i].iscsiIP, actual[i].iscsiIP);
                    Assert.AreEqual(expected[i].lastDeployedBIOS, actual[i].lastDeployedBIOS);
                    Assert.AreEqual(expected[i].ESXiPassword, actual[i].ESXiPassword);
                    Assert.AreEqual(expected[i].ESXiUsername, actual[i].ESXiUsername);
                    // thats enough for now
                }
            }
        }

        [TestMethod]
        public void canAllocateBlade()
        {
            using (bladeDirectorDebugServices uut = new bladeDirectorDebugServices(WCFPath, new[] { "1.1.1.1", "2.2.2.2", "3.3.3.3" }))
            {
                resultAndWaitToken requestStatus = uut.svcDebug._RequestAnySingleNode("192.168.1.1");
                Assert.AreEqual(resultCode.success, requestStatus.result.code);

                string[] allocated = uut.svc.getBladesByAllocatedServer("192.168.1.1");
                Assert.IsTrue(allocated.Contains("1.1.1.1"), "String '" + allocated + "' does not contain IP we allocated");
            }
        }

        [TestMethod]
        public void willReAllocateNode()
        {
            using (bladeDirectorDebugServices uut = new bladeDirectorDebugServices(WCFPath, new[] { "1.1.1.1" }))
            {
                uut.svc.setResourceSharingModel(fairnessChecker.fairnessType.allowAny);

                string hostip = "192.168.1.1";

                Assert.AreEqual(resultCode.success, uut.svcDebug._RequestAnySingleNode(hostip).result.code);

                // First, the node should be ours.
                Assert.AreEqual(GetBladeStatusResult.yours, uut.svcDebug._GetBladeStatus(hostip, "1.1.1.1"));

                // Then, someone else requests it..
                Assert.AreEqual(resultCode.pending, uut.svcDebug._RequestAnySingleNode("192.168.2.2").result.code);

                // and it should be pending.
                Assert.AreEqual(GetBladeStatusResult.releasePending, uut.svcDebug._GetBladeStatus(hostip, "1.1.1.1"));
                Assert.AreEqual(GetBladeStatusResult.releasePending, uut.svcDebug._GetBladeStatus("192.168.2.2", "1.1.1.1"));

                // Then, we release it.. 
                resultAndWaitToken res = uut.svcDebug._ReleaseBladeOrVM(hostip, "1.1.1.1", false);
                testUtils.waitForSuccess(uut, res, TimeSpan.FromSeconds(5));

                // and it should belong to the second requestor.
                Assert.AreEqual(GetBladeStatusResult.notYours, uut.svcDebug._GetBladeStatus(hostip, "1.1.1.1"));
                Assert.AreEqual(GetBladeStatusResult.yours, uut.svcDebug._GetBladeStatus("192.168.2.2", "1.1.1.1"));
            }
        }
         
        [TestMethod]
        public void willReAllocateNodeAfterTimeout()
        {
            using (bladeDirectorDebugServices uut = new bladeDirectorDebugServices(WCFPath, new[] { "1.1.1.1" }))
            {
                uut.svc.setResourceSharingModel(fairnessChecker.fairnessType.allowAny);

                uut.svcDebug.setKeepAliveTimeout(10);

                Assert.AreEqual(resultCode.success, uut.svcDebug._RequestAnySingleNode("192.168.1.1").result.code);
                Assert.AreEqual(resultCode.pending, uut.svcDebug._RequestAnySingleNode("192.168.2.2").result.code);

                // 1.1 has it, 2.2 is queued
                Assert.IsTrue(uut.svcDebug._isBladeMine("192.168.1.1", "1.1.1.1", false));
                Assert.IsFalse(uut.svcDebug._isBladeMine("192.168.2.2", "1.1.1.1", false));

                // Now let 1.1 timeout
                for (int i = 0; i < 11; i++)
                {
                    uut.svcDebug._keepAlive("192.168.2.2");
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                // and it should belong to the second requestor.
                Assert.IsFalse(uut.svcDebug._isBladeMine("192.168.1.1", "1.1.1.1", false));
                Assert.IsTrue(uut.svcDebug._isBladeMine("192.168.2.2", "1.1.1.1", false));
            }
        }

        [TestMethod]
        public void willTimeoutOnNoKeepalives()
        {
            using (bladeDirectorDebugServices uut = new bladeDirectorDebugServices(WCFPath, new[] { "1.1.1.1" }))
            {
                uut.svcDebug.setKeepAliveTimeout(10);
                
                string hostip = "192.168.1.1";

                resultAndWaitToken resp = uut.svcDebug._RequestAnySingleNode(hostip);
                resultAndBladeName resWithName = (resultAndBladeName) resp;
                Assert.AreEqual(resultCode.success, resp.result.code);
                Assert.AreEqual("1.1.1.1", resWithName.bladeName);
                Assert.AreEqual(GetBladeStatusResult.yours, uut.svcDebug._GetBladeStatus(hostip, resWithName.bladeName));
                Thread.Sleep(TimeSpan.FromSeconds(11));
                Assert.AreEqual(GetBladeStatusResult.unused, uut.svcDebug._GetBladeStatus(hostip, resWithName.bladeName));
            }
        }

        [TestMethod]
        public void willNotTimeoutWhenWeSendKeepalives()
        {
            using (bladeDirectorDebugServices uut = new bladeDirectorDebugServices(WCFPath, new[] { "1.1.1.1" }))
            {
                uut.svcDebug.setKeepAliveTimeout(10);
                string hostip = "192.168.1.1";

                Assert.AreEqual(resultCode.success, uut.svcDebug._RequestAnySingleNode(hostip).result.code);
                Assert.AreEqual(GetBladeStatusResult.yours, uut.svcDebug._GetBladeStatus(hostip, "1.1.1.1"));

                for (int i = 0; i < 11; i++)
                {
                    uut.svcDebug._keepAlive("192.168.1.1");
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
                Assert.AreEqual(GetBladeStatusResult.yours, uut.svcDebug._GetBladeStatus(hostip, "1.1.1.1"));
            }
        }

        [TestMethod]
        public void willEnforceSharing()
        {
            using (bladeDirectorDebugServices uut = new bladeDirectorDebugServices(WCFPath, new[] { "1.1.1.1", "1.1.1.2" }))
            {
                uut.svc.setResourceSharingModel(fairnessChecker.fairnessType.fair);

                string hostipA = "192.168.1.1";
                string hostipB = "192.168.1.2";

                // Host A will allocate all blades. Then, host B will request one. 
                // Host A should be asked to release one of its nodes, and then that should be allocated to B.
                Assert.AreEqual(resultCode.success, uut.svcDebug._RequestAnySingleNode(hostipA).result.code);
                Assert.AreEqual(resultCode.success, uut.svcDebug._RequestAnySingleNode(hostipA).result.code);

                // A owns both
                Assert.AreEqual(GetBladeStatusResult.yours, uut.svcDebug._GetBladeStatus(hostipA, "1.1.1.1"));
                Assert.AreEqual(GetBladeStatusResult.yours, uut.svcDebug._GetBladeStatus(hostipA, "1.1.1.2"));

                // B requests one
                Assert.AreEqual(resultCode.pending, uut.svcDebug._RequestAnySingleNode(hostipB).result.code);

                // A should be asked to release a blade.
                Assert.IsTrue(
                    uut.svcDebug._GetBladeStatus(hostipA, "1.1.1.1") == GetBladeStatusResult.releasePending ||
                    uut.svcDebug._GetBladeStatus(hostipA, "1.1.1.2") == GetBladeStatusResult.releasePending 
                    );

                // host A does as instructed, and releases its blade
                if (uut.svcDebug._GetBladeStatus(hostipA, "1.1.1.1") == GetBladeStatusResult.releasePending)
                {
                    resultAndWaitToken res = uut.svcDebug._ReleaseBladeOrVM(hostipA, "1.1.1.1", false);
                    testUtils.waitForSuccess(uut, res, TimeSpan.FromSeconds(5));
                }
                else if (uut.svcDebug._GetBladeStatus(hostipA, "1.1.1.2") == GetBladeStatusResult.releasePending)
                {
                    resultAndWaitToken res = uut.svcDebug._ReleaseBladeOrVM(hostipA, "1.1.1.2", false);
                    testUtils.waitForSuccess(uut, res, TimeSpan.FromSeconds(5));
                }
                else
                    throw new ArgumentException();

                // and now, each should have one blade.
                Assert.IsTrue(
                    (
                        uut.svcDebug._GetBladeStatus(hostipA, "1.1.1.1") == GetBladeStatusResult.yours &&
                        uut.svcDebug._GetBladeStatus(hostipB, "1.1.1.2") == GetBladeStatusResult.yours 
                    )
                    ||
                    (
                        uut.svcDebug._GetBladeStatus(hostipA, "1.1.1.2") == GetBladeStatusResult.yours &&
                        uut.svcDebug._GetBladeStatus(hostipB, "1.1.1.1") == GetBladeStatusResult.yours
                    )
                    );
            }
        }

        [TestMethod]
        public void willEnforceSharingWithVMsAndBlades()
        {
            using (bladeDirectorDebugServices uut = new bladeDirectorDebugServices(WCFPath, new[] { "1.1.1.1", "1.1.1.2" }))
            {
                string hostipA = "192.168.1.1";
                string hostipB = "192.168.1.2";

                // A requests all the VMs
                vmHWAndSWSpec toAlloc = new vmHWAndSWSpec( new VMHardwareSpec() { memoryMB = 4, cpuCount = 1 }, new VMSoftwareSpec());

                resultAndBladeName[] intialReqs = uut.svcDebug._requestAsManyVMAsPossible(hostipA, toAlloc.hw, toAlloc.sw);
                for (int index = 0; index < intialReqs.Length; index++)
                {
                    intialReqs[index] = (resultAndBladeName) testUtils.waitForSuccess(uut, intialReqs[index], TimeSpan.FromMinutes(1));
                }

                // Now B requests a blade
                Assert.AreEqual(resultCode.pending, uut.svcDebug._RequestAnySingleNode(hostipB).result.code);

                // A should be asked to release many VMs, and does as it is told
                int releaseRequestCount = 0;
                foreach (resultAndBladeName req in intialReqs)
                {
                    Debug.WriteLine(uut.svcDebug._GetVMStatus(hostipA, req.bladeName));
                    if (uut.svcDebug._GetVMStatus(hostipA, req.bladeName) == GetBladeStatusResult.releasePending)
                    {
                        resultAndWaitToken relWait = uut.svcDebug._ReleaseBladeOrVM(hostipA, req.bladeName, false);
                        testUtils.waitForSuccess(uut, relWait, TimeSpan.FromSeconds(30));

                        releaseRequestCount++;
                    }
                }
                Assert.AreEqual(12, releaseRequestCount);
                
                // Now, host B should have one blade.
                Assert.IsTrue(
                        uut.svcDebug._GetBladeStatus(hostipB, "1.1.1.2") == GetBladeStatusResult.yours ||
                        uut.svcDebug._GetBladeStatus(hostipB, "1.1.1.1") == GetBladeStatusResult.yours
                    );
            }
        }

        [TestMethod]
        public void willEnforceSharingWithVMs()
        {
            using (bladeDirectorDebugServices uut = new bladeDirectorDebugServices(WCFPath, new[] { "1.1.1.1", "1.1.1.2" }))
            {
                string hostipA = "192.168.1.1";
                string hostipB = "192.168.1.2";

                // Host A requests all the VMs
                vmHWAndSWSpec toAlloc = new vmHWAndSWSpec(
                    new VMHardwareSpec() { memoryMB = 4, cpuCount = 1 }, 
                    new VMSoftwareSpec());

                resultAndBladeName[] intialReqs = uut.svcDebug._requestAsManyVMAsPossible(hostipA, toAlloc.hw, toAlloc.sw);
                for (int index = 0; index < intialReqs.Length; index++)
                {
                    intialReqs[index] = (resultAndBladeName)testUtils.waitForSuccess(uut, intialReqs[index], TimeSpan.FromMinutes(1));
                }

                // Now B requests a VM
                resultAndBladeName reqFromB = uut.svcDebug._requestAnySingleVM(hostipB, toAlloc.hw, toAlloc.sw);
                Assert.AreEqual(resultCode.pending, reqFromB.result.code);

                // A should be asked to release a single VM, and does as it is told
                int releaseRequestCount = 0;
                foreach (resultAndBladeName req in intialReqs)
                {
                    Debug.WriteLine(uut.svcDebug._GetVMStatus(hostipA, req.bladeName));
                    if (uut.svcDebug._GetVMStatus(hostipA, req.bladeName) == GetBladeStatusResult.releasePending)
                    {
                        resultAndWaitToken relWait = uut.svcDebug._ReleaseBladeOrVM(hostipA, req.bladeName, false);
                        testUtils.waitForSuccess(uut, relWait, TimeSpan.FromSeconds(30));

                        releaseRequestCount++;
                    }
                }
                Assert.AreEqual(1, releaseRequestCount);

                // Now, host B should have one VM.
                resultAndWaitToken newVM = testUtils.waitForSuccess(uut, reqFromB, TimeSpan.FromSeconds(30));
                Assert.AreEqual(resultCode.success, newVM.result.code);
            }
        }

        [TestMethod]
        public void willReAllocateBladesAfterLogonDuringBIOSOperation()
        {
            using (bladeDirectorDebugServices svc = new bladeDirectorDebugServices(basicBladeTests.WCFPath))
            {
                string hostIP = "1.1.1.1";
                svc.svcDebug.initWithBladesFromIPList(new[] { "172.17.129.131" }, true, NASFaultInjectionPolicy.retunSuccessful);

                testUtils.doLogin(svc, hostIP);
                string bladeIP = testUtils.doBladeAllocationForTest(svc, hostIP);

                // Start a 5-minute long BIOS operation, then cancel it by logging in again.
                svc.svcDebug._setBIOSOperationTimeIfMocked((int)TimeSpan.FromMinutes(5).TotalSeconds);
                resultAndWaitToken res = svc.svcDebug._rebootAndStartDeployingBIOSToBlade(hostIP, bladeIP, ".... some bios file here ... ");
                Assert.AreEqual(resultCode.pending, res.result.code);

                Assert.AreEqual(true, svc.svcDebug._isBladeMine(hostIP, bladeIP, true));

                // Now login again, cancelling the BIOS operation.
                testUtils.doLogin(svc, hostIP);

                // The blade should no longer be ours.
                Assert.AreEqual(false, svc.svcDebug._isBladeMine(hostIP, bladeIP, false));

                // And after an allocation, our blade should be re-used.
                string newbladeIP = testUtils.doBladeAllocationForTest(svc, hostIP);
                Assert.AreEqual(bladeIP, newbladeIP);
            }
        }

        [TestMethod]
        public void wontLeakOnConnectionErrors()
        {
            using (bladeDirectorDebugServices uut = new bladeDirectorDebugServices(WCFPath, new[] {"1.1.1.1"}))
            {
                uut.setReceiveTimeout(TimeSpan.FromSeconds(4));

                // This will cause an exception..
                bool didExcept = false;
                try
                {
                    uut.svcDebug.lockAndSleep("1.1.1.1");
                }
                catch (CommunicationException)
                {
                    didExcept = true;
                }
                Assert.IsTrue(didExcept);
                uut.setReceiveTimeout(TimeSpan.FromSeconds(10));
                uut.reconnect();

                // But if we re-connect, locks should be untaken.
                uut.svcDebug._isBladeMine("127.0.0.1", "1.1.1.1", false);
            }
        }

        [TestMethod]
        public void wontLeakOnWebLeak()
        {
            Uri webURL = new Uri("http://localhost/" + Guid.NewGuid());
            using (bladeDirectorDebugServices uut = new bladeDirectorDebugServices(WCFPath, new[] { "127.0.0.1" }, false, webURL))
            {
                //uut.setReceiveTimeout(TimeSpan.FromSeconds(10));

                // This will leak a blade
                WebRequest request = WebRequest.Create(webURL + "/lockAndReturn");
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream respStream = response.GetResponseStream())
                    {
                        using (StreamReader respStreamReader = new StreamReader(respStream)) { }
                    }
                }

                // But if we re-connect, locks should be untaken.
                uut.svcDebug._isBladeMine("127.0.0.1", "127.0.0.1", false);
            }
        }

        [TestMethod]
        public void wontLeakOnHTTPErrors()
        {
            Uri webURL = new Uri("http://localhost/" + Guid.NewGuid());
            using (bladeDirectorDebugServices uut = new bladeDirectorDebugServices(WCFPath, new[] { "127.0.0.1" }, false, webURL))
            {
                for (int attempt = 0; attempt < 100; attempt++)
                {
                    uut.svcDebug._isBladeMine("127.0.0.1", "127.0.0.1", false);

                    Socket connSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                    connSock.Connect("localhost", 80);
                    Byte[] buf = Encoding.ASCII.GetBytes("GET http://localhost/test_wontLeakOnHTTPErrors/generateIPXEScript HTTP/1.1\n\n");
                    connSock.Send(buf);
                    connSock.Disconnect(true);

                    uut.svcDebug._isBladeMine("127.0.0.1", "127.0.0.1", false);
                }
            }
        }


        [TestMethod]
        public void canGenerateIPXEScriptForUnknownBlade()
        {
            Uri webURL = new Uri("http://localhost/" + Guid.NewGuid());
            using (bladeDirectorDebugServices uut = new bladeDirectorDebugServices(WCFPath, new[] { "1.1.1.1" }, false, webURL))
            {
                WebRequest request = WebRequest.Create(webURL + "/generateIPXEScript");
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream respStream = response.GetResponseStream())
                    {
                        using (StreamReader respStreamReader = new StreamReader(respStream))
                        {
                            string responseText = respStreamReader.ReadToEnd();
                            Assert.AreEqual("No blade at this IP address", responseText);
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void canGenerateIPXEScriptWithoutCrashing()
        {
            Uri webURL = new Uri("http://localhost/" + Guid.NewGuid());
            using (bladeDirectorDebugServices uut = new bladeDirectorDebugServices(WCFPath, new[] { "127.0.0.1" }, false, webURL))
            {
                WebRequest request = WebRequest.Create(webURL + "/generateIPXEScript");
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream respStream = response.GetResponseStream())
                    {
                        using (StreamReader respStreamReader = new StreamReader(respStream))
                        {
                            string responseText = respStreamReader.ReadToEnd();
                            Assert.IsTrue(responseText.Contains("Blade does not have any owner"),
                                "text 'Blade does not have any owner' not found in response '" + responseText + "'");
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void wontLeakOnUsingOwnershipTransfer()
        {
            // See bugzilla #101.
            // Here we are testing that resources won't leak, even when an asynchronous exception is thrown after the 
            // lockableBladeSpec is constructed, but _before_ the using has been entered.
            using (bladeDirectorDebugServices uut = new bladeDirectorDebugServices(WCFPath, new[] { "1.1.1.1" }))
            {
                uut.svcDebug._isBladeMine("127.0.0.1", "1.1.1.1", false);
                uut.svcDebug._isBladeMine("127.0.0.1", "1.1.1.1", false);
                uut.svcDebug._isBladeMine("127.0.0.1", "1.1.1.1", false);
                uut.setReceiveTimeout(TimeSpan.FromSeconds(4));

                // >:)
                uut.svcDebug.lockAndNeverRelease("1.1.1.1");

                uut.setReceiveTimeout(TimeSpan.FromSeconds(10));
                uut.reconnect();

                // But if we re-connect, locks should be untaken.
                uut.svcDebug._isBladeMine("127.0.0.1", "1.1.1.1", false);
            }
        }
    }
}
