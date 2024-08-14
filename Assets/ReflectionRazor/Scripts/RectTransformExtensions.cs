using UnityEngine;

namespace ReflectionRazor
{
	public static class RectTransformExtensions
	{
		public static Vector3 CenterPosition(this RectTransform self)
		{
			var position = self.position;
			var diff = new Vector3(
				Mathf.Lerp(-self.rect.size.x / 2f, self.rect.size.x / 2f, self.pivot.x) * self.transform.lossyScale.x,
				Mathf.Lerp(-self.rect.size.y / 2f, self.rect.size.y / 2f, self.pivot.y) * self.transform.lossyScale.y
			);
			return position - diff;
		}
	}
}