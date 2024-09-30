using Jammy.Assembler;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Collections.Generic;

namespace Jammy.Tests
{
	[TestFixture]
	public class AssemberTest
	{
		IAssembler assembler;

		[OneTimeSetUp]
		public void AssemblerTestInit()
		{
			assembler = new Assembler.Assembler();
		}
		
		//Dx
		//Ax
		//(Ax)
		//(Ax)+
		//-(Ax)
		//d16(Ax)
		//d8(Ax,Xn)  Xn is Ax/Ax.w or Dx/Dx.w
		//d16(pc)
		//d8(pc,Xn)
		//(xxx).w
		//(xxx).l
		//#imm

		private readonly List<string> validEA =
			["D0",
			"A1",
			"(a2)+",
			"-(a3)",
			"5(a1)",
			"$7fff(a2)",
			"$7f(a2,d0.w)",
			"$1f(a2,d5.l)",
			"$2f(a2,a0.w)",
			"$3f(a2,a6.l)",
			"$7fff(pc)",
			"$7fff(pc,d0)",
			"(123).W",
			"(123456).l",
			"#$7fffffff",
			"#-2000000000",
			];

		[Test]
		public void TestEA()
		{
			foreach (var ea in validEA)
			{
				string program = $"fadd.b {ea},fp7";
				var asm = assembler.Assemble(program);

				ClassicAssert.IsFalse(asm.HasErrors());
			}
		}
	}
}
