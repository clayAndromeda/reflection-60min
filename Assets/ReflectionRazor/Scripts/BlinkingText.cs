using TMPro;
using UnityEngine;

namespace ReflectionRazor
{
	/// <summary>
	/// 点滅させる
	/// </summary>
	[RequireComponent(typeof(TMP_Text))]
	public class BlinkingText : MonoBehaviour
	{
		TMP_Text textComponent;
		
		private void Awake()
		{
			textComponent = GetComponent<TMP_Text>();
		}
		
		private void Update()
		{
			textComponent.color = new Color(
				textComponent.color.r,
				textComponent.color.g,
				textComponent.color.b,
				Mathf.PingPong(Time.time, 0.5f)
			);
		}
	}
}