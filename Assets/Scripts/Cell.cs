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

	private GridLayoutGroup twoDroneGridLayout;
	private GridLayoutGroup fourDroneGridLayout;

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
		twoDroneGridLayout = twoDroneParent.GetComponent<GridLayoutGroup>();
		fourDroneGridLayout = fourDroneParent.GetComponent<GridLayoutGroup>();
	}

	public void UpdateGridLayoutGroup(Vector2 cellSize)
	{
		float cellSizeX = cellSize.x;
		cellSizeX -= twoDroneGridLayout.padding.left + twoDroneGridLayout.padding.right;

		float cellSizeY = cellSize.y - twoDroneGridLayout.spacing.y;
		cellSizeY /= 2;
		cellSizeY -= twoDroneGridLayout.padding.top;

		twoDroneGridLayout.cellSize = new Vector2(cellSizeX, cellSizeY);

		cellSizeX = cellSize.x - fourDroneGridLayout.spacing.x;
		cellSizeX /= 2;
		cellSizeX -= fourDroneGridLayout.padding.left;

		cellSizeY = cellSize.y - fourDroneGridLayout.spacing.y;
		cellSizeY /= 2;
		cellSizeY -= fourDroneGridLayout.padding.top;

		fourDroneGridLayout.cellSize = new Vector2(cellSizeX, cellSizeY);	
	}
}
