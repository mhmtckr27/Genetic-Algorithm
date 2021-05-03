using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Cell : MonoBehaviour
{
	[SerializeField] public Text droneLocationText;
	[SerializeField] public GameObject oneDroneParent;
	[SerializeField] public GameObject twoDroneParent;
	[SerializeField] public GameObject fourDroneParent;

	[HideInInspector] public bool isExplored;
	[HideInInspector] public List<GameObject> oneDroneImages;
	[HideInInspector] public List<GameObject> twoDroneImages;
	[HideInInspector] public List<GameObject> fourDroneImages;

	private void Awake()
	{
		for (int i = 0; i < oneDroneParent.transform.childCount; i++)
		{
			oneDroneImages.Add(oneDroneParent.transform.GetChild(i).gameObject);
		}
		for (int i = 0; i < twoDroneParent.transform.childCount; i++)
		{
			twoDroneImages.Add(twoDroneParent.transform.GetChild(i).gameObject);
		}
		for(int i = 0; i < fourDroneParent.transform.childCount; i++)
		{
			fourDroneImages.Add(fourDroneParent.transform.GetChild(i).gameObject);
		}
	}
}
