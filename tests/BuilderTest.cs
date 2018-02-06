using System.Text;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GolombCodeFilterSet;

namespace GolombCodedFilterSet.UnitTests
{
	[TestClass]
	public class BuilderTest
	{
		[TestMethod]
		public void BuildFilterAndMatchValues()
		{
			var names = from name in new[] { "New York", "Amsterdam", "Paris", "Buenos Aires", "La Habana" }
						select Encoding.ASCII.GetBytes(name);

			var builder = new FilterBuilder(0x10);
			var key = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
			var filter = builder.Build(key, names);

			// The filter should match all ther values that were added
			foreach(var name in names)
			{
				Assert.IsTrue(filter.Match(name, key));
			}

			// The filter should NOT match any extra value
			Assert.IsFalse(filter.Match(Encoding.ASCII.GetBytes("Porto Alegre"), key));
			Assert.IsFalse(filter.Match(Encoding.ASCII.GetBytes("Madrid"), key));
		}
	}
}
