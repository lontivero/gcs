using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GolombCodeFilterSet;
using MagicalCryptoWallet.Backend;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GolombCodedFilterSet.UnitTests
{
	[TestClass]
	public class StoreTest
	{
		[TestMethod]
		public void Test1()
		{
			var stream = new MemoryStream();
			var filterStore = new GcsFilterStore(stream);
			filterStore.Put(new GcsFilter(new FastBitArray(), 20, 10));
			filterStore.Put(new GcsFilter(new FastBitArray(), 20, 35));

			stream.Seek(0, SeekOrigin.Begin);
			var filters = filterStore.ToArray();
			Assert.AreEqual(2, filters.Length);
			Assert.AreEqual(10, filters[0].N);
			Assert.AreEqual(35, filters[1].N);
		}
	}
}
