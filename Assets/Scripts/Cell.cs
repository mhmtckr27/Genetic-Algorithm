using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Cell : MonoBehaviour, IPointerClickHandler
{
	[SerializeField] public Text droneLocationText;
	[SerializeField] public GameObject exploredByDroneImagesParent;
	//[SerializeField] public GameObject twoDroneParent;
	//[SerializeField] public GameObject fourDroneParent;

	[HideInInspector] public bool isExplored;
	[HideInInspector] public List<UnityEngine.UI.Image> exploredByDroneImages;

	public int rowNumber;
	public int columnNumber;

	public Vector2 pixelPosition;

	//[HideInInspector] public List<GameObject> twoDroneImages;
	//[HideInInspector] public List<GameObject> fourDroneImages;

	//private GridLayoutGroup twoDroneGridLayout;
	//private GridLayoutGroup fourDroneGridLayout;

	public void Init(int rowNumber, int columnNumber)
	{
		this.rowNumber = rowNumber;
		this.columnNumber = columnNumber;
		pixelPosition = transform.position;
	}

	private void Awake()
	{
		for (int i = 0; i < exploredByDroneImagesParent.transform.childCount; i++)
		{
			exploredByDroneImages.Add(exploredByDroneImagesParent.transform.GetChild(i).GetComponent<UnityEngine.UI.Image>());
		}
		//pixelPosition = GetComponent<RectTransform>().anchoredPosition;
	/*	for (int i = 0; i < twoDroneParent.transform.childCount; i++)
		{
			twoDroneImages.Add(twoDroneParent.transform.GetChild(i).gameObject);
		}
		for(int i = 0; i < fourDroneParent.transform.childCount; i++)
		{
			fourDroneImages.Add(fourDroneParent.transform.GetChild(i).gameObject);
		}
		twoDroneGridLayout = twoDroneParent.GetComponent<GridLayoutGroup>();
		fourDroneGridLayout = fourDroneParent.GetComponent<GridLayoutGroup>();*/
	}

	/*public void UpdateGridLayoutGroup(Vector2 cellSize)
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
	}*/

	public void ExploreByDrones(List<int> droneNumbers)
	{
		ResetExploreds();
		if(droneNumbers.Count == 0) { return; }
		float fillAmount = (float)1 / droneNumbers.Count;
		for(int i = 0; i < droneNumbers.Count; i++)
		{
			exploredByDroneImages[droneNumbers[i]].fillAmount = 1 - i * fillAmount;
			exploredByDroneImages[droneNumbers[i]].gameObject.SetActive(true);
		}
	}

	public void ResetExploreds()
	{
		for(int i = 0; i < exploredByDroneImages.Count; i++)
		{
			exploredByDroneImages[i].gameObject.SetActive(false);
		}
	}

	public void OnPointerClick(PointerEventData eventData) // 3
	{
		GameManager.Instance.TrySetStartPosition(rowNumber, columnNumber);
	}
}
