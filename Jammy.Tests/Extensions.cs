using Moq;
using System;
using System.Linq.Expressions;

namespace Jammy.Tests
{
	public static class MoqFluentExtensions
	{
		public static Mock<T> Setup<T, TResult>(
			this Mock<T> mock,
			Expression<Func<T, TResult>> expr,
			Func<TResult> valueFactory) where T : class
		{
			mock.Setup(expr).Returns(valueFactory);
			return mock;
		}
	}
}
