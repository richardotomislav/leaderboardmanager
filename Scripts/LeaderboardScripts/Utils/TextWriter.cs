using UnityEngine;
using TMPro;

namespace LeaderbordManager
{
	public class TextWriter : MonoBehaviour
	{
		[SerializeField] string prefix = "";
		[SerializeField] string suffix = "";
		string text;

		public string Text
		{
			get => text;
			set
			{
				text = value;
				Set(text);
			}
		}

		TextMeshProUGUI textMesh;

		// Start is called before the first frame update
		void Awake()
		{
			textMesh = gameObject.GetComponent<TextMeshProUGUI>();
		}


		public void Set(string newText)
		{
			SetString(newText);
		}

		public void Set(int @int)
		{
			SetString(@int.ToString());
		}

		public void Set(Color newColor)
		{
			textMesh.color = newColor;
		}

		public void Set(float @float, string format)
		{
			SetString(@float.ToString(format));
		}

		void SetString(string value)
		{
			string result = prefix + value + suffix;
			result = result.Replace("\r", "");
			textMesh.text = result;

		}

		public void SetItalic(bool italic)
		{
			// string currentText = textMesh.text;
			textMesh.fontStyle = italic ? FontStyles.Italic : FontStyles.Normal;
		}
	}
}
