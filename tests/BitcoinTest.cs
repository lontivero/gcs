using GolombCodeFilterSet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBitcoin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GolombCodedFilterSet.UnitTests
{
	[TestClass]
	public class BitcoinTest
	{
		[TestMethod]
		public void PerformanceTestWithRealData()
		{
			var masterKey = new ExtKey();

			// The algorhytm must approximately check for 2 million addresses within 700 blocks, assumbing blocks are about 700MB.
			// These numbers are based on the sample taken from block height 407000 to 407701.
			var numberOfElements = 2000000;
			var numberOfCheckAgainst = 1000; // Estimate that our user will have 1000 addresses.

			// <--- PROBLEM --->
			// 
			// This test takes: 
			// numberOfElements: 2 000, numberOfCheckAgainst: 1 is 3 sec
			// numberOfElements: 20 000, numberOfCheckAgainst: 10 is 1 min
			//
			// <--- PROBLEM --->

			var elements = new List<byte[]>();
			var checkAgainst = new List<byte[]>();
			for (uint i = 0; i < numberOfElements; i++)
			{
				elements.Add(masterKey.Derive(0).Derive(i).Neuter().ScriptPubKey.ToCompressedBytes());
			}

			
			for (uint i = 0; i < numberOfCheckAgainst; i++)
			{
				elements.Add(masterKey.Derive(1).Derive(i).Neuter().ScriptPubKey.ToCompressedBytes());
			}

			var key = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
			var filter = Filter.Build(key, 0x10, elements);

			// The filter should match all ther values that were added
			foreach (var scriptPubKey in elements)
			{
				Assert.IsTrue(filter.Match(scriptPubKey, key));
			}

			var falsePositiveCount = 0;
			foreach(var scriptPubKey in checkAgainst)
			{
				if(filter.Match(scriptPubKey, key))
				{
					falsePositiveCount++;
				}
			}

			Assert.IsTrue(falsePositiveCount < 100);
		}
	}
}
