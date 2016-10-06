﻿using System;
using System.Linq;
using System.Threading;
using bladeDirector;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void canInitWithBladesAndGetListBack()
        {
            bladeDirector.services.initWithBlades(new[] { "1.1.1.1", "2.2.2.2", "3.3.3.3" });

            services uut = new bladeDirector.services();

            string res = uut.ListNodes();
            string[] foundIPs = res.Split(',');
            Assert.AreEqual(3, foundIPs.Length);
            Assert.IsTrue(foundIPs.Contains("1.1.1.1"));
            Assert.IsTrue(foundIPs.Contains("2.2.2.2"));
            Assert.IsTrue(foundIPs.Contains("3.3.3.3"));
        }

        [TestMethod]
        public void canGetBladeSpec()
        {
            bladeSpec spec1Expected = new bladeSpec("blade1ip", "blade1iscsiIP", "blade1ILOIP", 111);
            bladeSpec spec2Expected = new bladeSpec("blade2ip", "blade2iscsiIP", "blade2ILOIP", 222);
            bladeDirector.services.initWithBlades(new [] { spec1Expected, spec2Expected });

            services uut = new bladeDirector.services();

            bladeSpec spec1Actual = uut.getConfigurationOfBlade("blade1ip");
            bladeSpec spec2Actual = uut.getConfigurationOfBlade("blade2ip");

            Assert.AreEqual(spec1Expected, spec1Actual);
            Assert.AreEqual(spec2Expected, spec2Actual);
        }

        [TestMethod]
        public void canAllocateBlade()
        {
            bladeDirector.services.initWithBlades(new[] { "1.1.1.1", "2.2.2.2", "3.3.3.3" });
            services uut = new bladeDirector.services();

            Assert.AreEqual(resultCode.success.ToString(), uut.RequestNode("1.1.1.1", "192.168.1.1"));

            string allocated = uut.getBladesByAllocatedServer("192.168.1.1");
            Assert.IsTrue(allocated.Contains("1.1.1.1"), "String '" + allocated + "' does not contiain IP we allocated");
        }
        
        [TestMethod]
        public void willReAllocateNode()
        {
            bladeDirector.services.initWithBlades(new[] { "1.1.1.1", "2.2.2.2", "3.3.3.3" });
            services uut = new bladeDirector.services();

            Assert.AreEqual(resultCode.success.ToString(), uut.RequestNode("1.1.1.1", "192.168.1.1"));

            // First, the node should be ours.
            Assert.AreEqual(GetBladeStatusResult.yours.ToString(), uut.GetBladeStatus("1.1.1.1", "192.168.1.1"));

            // Then, someoene else requests it..
            Assert.AreEqual(resultCode.pending.ToString(), uut.RequestNode("1.1.1.1", "192.168.2.2"));

            // and it should be pending.
            Assert.AreEqual(GetBladeStatusResult.releasePending.ToString(), uut.GetBladeStatus("1.1.1.1", "192.168.1.1"));
            Assert.AreEqual(GetBladeStatusResult.releasePending.ToString(), uut.GetBladeStatus("1.1.1.1", "192.168.2.2"));

            // Then, we release it.. 
            Assert.AreEqual(resultCode.success.ToString(), uut.releaseBlade("1.1.1.1", "192.168.1.1"));

            // and it should belong to the second requestor.
            Assert.AreEqual(GetBladeStatusResult.notYours.ToString(), uut.GetBladeStatus("1.1.1.1", "192.168.1.1"));
            Assert.AreEqual(GetBladeStatusResult.yours.ToString(), uut.GetBladeStatus("1.1.1.1", "192.168.2.2"));
        }

        [TestMethod]
        public void willForceReAllocation()
        {
            bladeDirector.services.initWithBlades(new[] {"1.1.1.1", "2.2.2.2", "3.3.3.3"});
            services uut = new bladeDirector.services();

            Assert.AreEqual(resultCode.success.ToString(), uut.RequestNode("1.1.1.1", "192.168.1.1"));

            Assert.AreEqual(GetBladeStatusResult.yours.ToString(), uut.GetBladeStatus("1.1.1.1", "192.168.1.1"));

            uut.forceBladeAllocation("1.1.1.1", "192.168.2.2");

            Assert.AreEqual(GetBladeStatusResult.notYours.ToString(), uut.GetBladeStatus("1.1.1.1", "192.168.1.1"));
            Assert.AreEqual(GetBladeStatusResult.yours.ToString(), uut.GetBladeStatus("1.1.1.1", "192.168.2.2"));
        }

        [TestMethod]
        public void willTimeoutOnNoKeepalives()
        {
            bladeDirector.services.initWithBlades(new[] { "1.1.1.1" });
            services uut = new bladeDirector.services();

            Assert.AreEqual(resultCode.success.ToString(), uut.RequestNode("1.1.1.1", "192.168.1.1"));
            Assert.AreEqual(GetBladeStatusResult.yours.ToString(), uut.GetBladeStatus("1.1.1.1", "192.168.1.1"));
            Thread.Sleep(TimeSpan.FromSeconds(61));
            Assert.AreEqual(GetBladeStatusResult.unused.ToString(), uut.GetBladeStatus("1.1.1.1", "192.168.1.1"));
        }

        [TestMethod]
        public void willNotTimeoutWhenWeSendKeepalives()
        {
            bladeDirector.services.initWithBlades(new[] { "1.1.1.1" });
            services uut = new bladeDirector.services();

            Assert.AreEqual(resultCode.success.ToString(), uut.RequestNode("1.1.1.1", "192.168.1.1"));
            Assert.AreEqual(GetBladeStatusResult.yours.ToString(), uut.GetBladeStatus("1.1.1.1", "192.168.1.1"));

            for (int i = 0; i < 61; i++)
            {
                uut.keepAlive("192.168.1.1");
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            Assert.AreEqual(GetBladeStatusResult.yours.ToString(), uut.GetBladeStatus("1.1.1.1", "192.168.1.1"));
        }
    }
}