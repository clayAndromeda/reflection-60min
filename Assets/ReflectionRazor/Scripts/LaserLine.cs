using UnityEngine;

namespace ReflectionRazor
{
	public class LaserLine : MonoBehaviour
	{
		[SerializeField] private LineRenderer lineRenderer;

		public void DrawLine(Vector3 startPoint, Vector3 endPoint)
		{
			lineRenderer.positionCount = 2;
			lineRenderer.SetPosition(0, startPoint);
			lineRenderer.SetPosition(1, endPoint);
		}
	}
}