using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vectrosity;

/// <summary>
/// Author: Mehmet Hayri Çakýr
/// </summary>


public class GameManager : MonoBehaviour
{
	private const int rowColumnCountDefault = 9;
	private const int droneCountDefault = 1;
	private const int populationCountDefault = 1000;
	private const int selectCountDefault = 32;
	private const float exploredAreaFitnessWeightDefault = 0.55f;
	private const float returnedToStartFitnessWeightDefault = 0.35f;
	private const float rotateFitnessWeightDefault = 0.1f;
	private const bool stopIfExploredAllCellsDefault = false;
	private const bool stopIfReachedDesiredFitnessDefault = false;
	private const bool stopIfReachedMaxGenerationCountDefault = true;
	private const bool drawDronePathsEachGenerationDefault = false;
	private const bool drawDronePathsBestGenerationDefault = false;
	private const float desiredFitnessDefault = 0.95f;
	private const int maxGenerationCountDefault = 300;
	private const int reproduceFunctionOptionDefault = 3;
	private const int mutationCountDefault = 2;

	[Space]
	[Header("Grid Map")]
	[SerializeField] private GameObject gridObject;
	[SerializeField] private GameObject cellPrefab;
	[SerializeField] private int cellRowColumnCount;
	private GridLayoutGroup gridLayoutGroup;
	private List<List<Cell>> cells;

	[Space]
	[Header("Drones")]
	[SerializeField] private int droneCount;
	[SerializeField] private GameObject startFinishLocationImagePrefab;
	[SerializeField] private Vector2Int startFinishLocation;
	private List<int> moveDirections = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
	private List<Vector2Int> directionCoordinates = new List<Vector2Int> {new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, -1),
																	new Vector2Int(0, 1), new Vector2Int(1, -1), new Vector2Int(1, 0), new Vector2Int(1, 1)};
	private List<int> moveRotateFitnesses = new List<int> { 4, 5, 4, 3, 3, 2, 1, 2 };
	//private List<Drone> drones = new List<Drone>();
	/// <directions>
	/// 0	1	2
	/// 3	X	4
	/// 5	6	7
	/// </directions>

	[Space]
	[Header("Genetic Algorithm")]
	[SerializeField] private int populationCount;
	[SerializeField] [Tooltip("Best selectCount individuals will be used to reproduce from.")] private int selectCount;
	[SerializeField] private int reproduceFunctionOption;
	[SerializeField] private int mutationCount;
	private int chromosomeLength;
	private int currentGeneration;
	private int maxExploredCellCount;

	[Space]
	[Header("Fitness Functions")]
	[SerializeField] private float exploredAreaFitnessWeight;
	[SerializeField] private float returnedToStartFitnessWeight;
	[SerializeField] private float rotateFitnessWeight;

	[Space]
	[Header("Stopping Conditions")]
	[SerializeField] private bool stopIfExploredAllCells;
	[SerializeField] private bool stopIfReachedDesiredFitness;
	[SerializeField] private bool stopIfReachedMaxGenerationCount;
	[SerializeField] private float desiredFitness;
	[SerializeField] private int maxGenerationCount;

	[Space]
	[Header("UI")]
	[SerializeField] private bool drawDronePathsEachGeneration;
	[SerializeField] private bool drawDronePathsBestGeneration;
	[SerializeField] private Text runButtonText;
	[SerializeField] private Text stopButtonText;
	[SerializeField] private Text generationText;
	[SerializeField] private Text bestFitnessOfThisGenerationText;
	[SerializeField] private Text bestFitnessOfAllGenerationsText;
	[SerializeField] private Text currentGenExploredCellsCountText;
	[SerializeField] private Text maxExploredCellsCountText;
	[SerializeField] private Text timeElapsedText;
	[SerializeField] private List<Color> droneColors;

	[Space]
	[Header("Parameters Panel UI")]
	[SerializeField] private Dropdown rowColumnCountDropdown;
	[SerializeField] private Dropdown droneCountDropDown;
	[SerializeField] private InputField populationCountInputField;
	[SerializeField] private InputField selectCountInputField;
	[SerializeField] private InputField exploredAreaFitnessWeightInputField;
	[SerializeField] private InputField returnedToStartFitnessWeightInputField;
	[SerializeField] private InputField rotateFitnessWeightInputField;
	[SerializeField] private Dropdown drawDronePathsDropdown;
	[SerializeField] private ToggleGroupPublicToggles stoppingConditionsToggleGroup;
	[SerializeField] private InputField desiredFitnessInputField;
	[SerializeField] private InputField maxGenerationCountInputField;
	

	//private VectorLine[] lines;
	private List<VectorLine> lines = new List<VectorLine>();
	private int lastMovedDirection = 1;
	List<Individual> population;
	private AlgorithmState algorithmState;
	private GameObject startFinishLocationObject;
	public enum AlgorithmState
	{
		Stopped,
		Running,
		Paused,
		Finished
	}

	private static GameManager instance;
	public static GameManager Instance
	{
		get => instance;
	}

	private void Awake()
	{
		if(instance == null)
		{
			instance = this;
		}
		else if(instance != this)
		{
			Destroy(gameObject);
		}
		gridLayoutGroup = gridObject.GetComponent<GridLayoutGroup>();
	}

	private void Start()
	{
		cells = new List<List<Cell>>();

		RestoreDefaults();
		InitGridMap();
	}


	public void OnRunButton()
	{
		if(algorithmState == AlgorithmState.Stopped)
		{
			OnStopButton();
			SetParameters();
			algorithmState = AlgorithmState.Running;
			runButtonText.text = "PAUSE";
			StartCoroutine(InitFirstGeneration(population));
			StartCoroutine(UpdateTimeElapsed());
		}
		else if(algorithmState == AlgorithmState.Paused)
		{
			algorithmState = AlgorithmState.Running;
			runButtonText.text = "PAUSE";
		}
		else if(algorithmState == AlgorithmState.Running)
		{
			algorithmState = AlgorithmState.Paused;
			runButtonText.text = "CONTINUE";
		}
		else if(algorithmState == AlgorithmState.Finished)
		{
			algorithmState = AlgorithmState.Stopped;
			OnRunButton();
		}
	}

	private void SetParameters()
	{
		cellRowColumnCount = rowColumnCountDropdown.value + 1;
		droneCount = droneCountDropDown.value + 1;
		populationCount = int.Parse(populationCountInputField.text);
		selectCount = int.Parse(selectCountInputField.text);
		exploredAreaFitnessWeight = float.Parse(exploredAreaFitnessWeightInputField.text);
		returnedToStartFitnessWeight = float.Parse(returnedToStartFitnessWeightInputField.text);
		rotateFitnessWeight = float.Parse(rotateFitnessWeightInputField.text);
		drawDronePathsEachGeneration = drawDronePathsDropdown.value == 1;
		drawDronePathsBestGeneration = drawDronePathsDropdown.value == 2;

		stopIfExploredAllCells = stoppingConditionsToggleGroup.GetToggles()[1].isOn;
		stopIfReachedDesiredFitness = stoppingConditionsToggleGroup.GetToggles()[2].isOn;
		stopIfReachedMaxGenerationCount = stoppingConditionsToggleGroup.GetToggles()[0].isOn;

		//Debug.LogError(stopIfExploredAllCells);
		//Debug.LogError(stopIfReachedDesiredFitness);
		//Debug.LogError(stopIfReachedMaxGenerationCount);

		desiredFitness = float.Parse(desiredFitnessInputField.text);
		maxGenerationCount = int.Parse(maxGenerationCountInputField.text);

		mutationCount = mutationCountDefault;
	}

	private Vector2 CalculateCellSize(int cellRowColumnCount)
	{
		float gridObjectSizeX = gridObject.GetComponent<RectTransform>().sizeDelta.x;
		gridObjectSizeX -= gridLayoutGroup.padding.left + gridLayoutGroup.padding.right;
		gridObjectSizeX -= (cellRowColumnCount - 1) * gridLayoutGroup.spacing.x;
		gridObjectSizeX /= cellRowColumnCount;
		Vector2 cellSize = new Vector2(gridObjectSizeX, gridObjectSizeX);
		
		return cellSize;
	}
	private IEnumerator CoWaitForPosition(int cellRowColumnCount)
	{
		yield return new WaitForEndOfFrame();
		// Find position of objects in grid
		for (int i = 0; i < cellRowColumnCount; i++)
		{
			for (int j = 0; j < cellRowColumnCount; j++)
			{
				cells[i][j].Init(i, j);
			}
		}
	}

	public void UpdateGridMap(Dropdown newValue)
	{
		if(cells.Count == 0) { return; }
		OnStopButton();
		cellRowColumnCount = newValue.value + 1;
		for (int i = 0; i < cells.Count; i++)
		{
			for (int j = 0; j < cells.Count; j++)
			{
				GameObject temp = cells[i][j].gameObject;
				Destroy(temp);
			}
		}
		cells.Clear();
		cells = new List<List<Cell>>();
		for (int i = 0; i < cellRowColumnCount; i++)
		{
			cells.Add(new List<Cell>());
			for (int j = 0; j < cellRowColumnCount; j++)
			{
				cells[i].Add(Instantiate(cellPrefab, gridObject.transform).GetComponent<Cell>());
			}
		}

		StartCoroutine(CoWaitForPosition(cellRowColumnCount));
		algorithmState = AlgorithmState.Stopped;
		RotateMoveDirections(1);
		gridLayoutGroup.cellSize = CalculateCellSize(cellRowColumnCount);

		Destroy(startFinishLocationObject);
		if(((startFinishLocation.x + 1) > cells.Count) || ((startFinishLocation.y + 1) > cells.Count))
		{
			startFinishLocationObject = Instantiate(startFinishLocationImagePrefab, cells[cellRowColumnCount - 1][cellRowColumnCount - 1].transform, false);
			TrySetStartPosition(cellRowColumnCount - 1, cellRowColumnCount - 1);
		}
		else
		{
			startFinishLocationObject = Instantiate(startFinishLocationImagePrefab, cells[startFinishLocation.x][startFinishLocation.y].transform, false);
		}
	}

	private void InitGridMap()
	{
		gridLayoutGroup.cellSize = CalculateCellSize(cellRowColumnCount);

		for (int i = 0; i < cellRowColumnCount; i++)
		{
			cells.Add(new List<Cell>());
			for (int j = 0; j < cellRowColumnCount; j++)
			{
				cells[i].Add(Instantiate(cellPrefab, gridObject.transform).GetComponent<Cell>());
			}
		}

		StartCoroutine(CoWaitForPosition(cellRowColumnCount));
		algorithmState = AlgorithmState.Stopped;
		RotateMoveDirections(1);

		startFinishLocationObject = Instantiate(startFinishLocationImagePrefab, cells[startFinishLocation.x][startFinishLocation.y].transform, false);
	}

	public void RestoreDefaults()
	{
		if((algorithmState == AlgorithmState.Stopped) || (algorithmState == AlgorithmState.Finished))
		{
			reproduceFunctionOption = reproduceFunctionOptionDefault;

			cellRowColumnCount = rowColumnCountDefault;
			droneCount = droneCountDefault;
			populationCount = populationCountDefault;
			selectCount = selectCountDefault;
			exploredAreaFitnessWeight = exploredAreaFitnessWeightDefault;
			returnedToStartFitnessWeight = returnedToStartFitnessWeightDefault;
			rotateFitnessWeight = rotateFitnessWeightDefault;
			mutationCount = mutationCountDefault;

			stopIfExploredAllCells = stopIfExploredAllCellsDefault;
			stopIfReachedDesiredFitness = stopIfReachedDesiredFitnessDefault;
			stopIfReachedMaxGenerationCount = stopIfReachedMaxGenerationCountDefault;

			desiredFitness = desiredFitnessDefault;
			maxGenerationCount = maxGenerationCountDefault;
			drawDronePathsEachGeneration = drawDronePathsEachGenerationDefault;
			drawDronePathsBestGeneration = drawDronePathsBestGenerationDefault;

			rowColumnCountDropdown.value = rowColumnCountDefault - 1;
			droneCountDropDown.value = droneCountDefault - 1;
			populationCountInputField.text = populationCountDefault.ToString();
			selectCountInputField.text = selectCountDefault.ToString();
			exploredAreaFitnessWeightInputField.text = exploredAreaFitnessWeightDefault.ToString("F2");
			returnedToStartFitnessWeightInputField.text = returnedToStartFitnessWeightDefault.ToString("F2");
			rotateFitnessWeightInputField.text = rotateFitnessWeightDefault.ToString("F2");

			if (stopIfExploredAllCellsDefault)
			{
				stoppingConditionsToggleGroup.GetToggles()[2].isOn = true;
			}
			else if (stopIfReachedDesiredFitnessDefault)
			{
				stoppingConditionsToggleGroup.GetToggles()[1].isOn = true;
			}
			else if (stopIfReachedMaxGenerationCountDefault)
			{
				stoppingConditionsToggleGroup.GetToggles()[0].isOn = true;
			}

			if (drawDronePathsEachGenerationDefault)
			{
				drawDronePathsDropdown.value = 1;
			}
			else if (drawDronePathsBestGenerationDefault)
			{
				drawDronePathsDropdown.value = 2;
			}
			else
			{
				drawDronePathsDropdown.value = 0;
			}

			desiredFitnessInputField.text = desiredFitnessDefault.ToString("F2");
			maxGenerationCountInputField.text = maxGenerationCountDefault.ToString();
		}
	}

	public void OnStopButton()
	{
		for(int i = 0; i < lines.Count; i++)
		{
			VectorLine.lineManager.DisableLine(lines[i], 0f);
		}
		lines.Clear();

		algorithmState = AlgorithmState.Stopped;
		population = new List<Individual>();
		maxExploredCellCount = 0;
		RotateMoveDirections(1);
		for (int i = 0; i < cellRowColumnCount; i++)
		{
			for (int j = 0; j < cellRowColumnCount; j++)
			{
				cells[i][j].droneLocationText.text = "";
				cells[i][j].ResetExploreds();
			}
		}


		generationText.text = "0";
		bestFitnessOfThisGenerationText.text = "0";
		bestFitnessOfAllGenerationsText.text = "0";
		currentGenExploredCellsCountText.text = "0";
		maxExploredCellsCountText.text = "0";
		runButtonText.text = "RUN";
		stopButtonText.text = "STOP";
	}

	private IEnumerator UpdateTimeElapsed()
	{
		float startingTime = Time.time;
		int hours = 0;
		int minutes = 0;
		float seconds = 0;
		float elapse;
		float currentTime;
		while (algorithmState != AlgorithmState.Stopped)
		{
			yield return null;
			currentTime = Time.time;
			elapse = currentTime - startingTime;
			startingTime = currentTime;
			if(algorithmState == AlgorithmState.Running)
			{
				seconds += elapse;
				if (seconds >= 60)
				{
					minutes++;
					seconds %= 60;
				}
				if (minutes >= 60)
				{
					hours++;
					minutes %= 60;
				}
				timeElapsedText.text = hours.ToString("D2") + ":" + minutes.ToString("D2") + ":" + seconds.ToString("00.0");
			}
		}
		if(algorithmState == AlgorithmState.Stopped)
		{
			timeElapsedText.text = "00:00:00,0";
		}
		yield return null;
	}

	public void TrySetStartPosition(int cellRow, int cellColumn)
	{
		if ((algorithmState == AlgorithmState.Stopped))
		{
			startFinishLocation = new Vector2Int(cellRow, cellColumn);
			startFinishLocationObject.transform.SetParent(cells[cellRow][cellColumn].transform, false);
		}
	}

	private IEnumerator InitFirstGeneration(List<Individual> population)
	{
		chromosomeLength = Mathf.CeilToInt((cellRowColumnCount * cellRowColumnCount - 1) / droneCount) + 1;
		currentGeneration = 1;


		for (int j = 0; j < populationCount; j++)
		{
			population.Add(GenerateRandomChromosome(chromosomeLength * droneCount));
		}
		CalculateFitnesses(population);
		RotateMoveDirections(1);
		yield return StartCoroutine(GeneticAlgorithm(population));
		yield return null;
	}

	private IEnumerator UpdateUI(Individual bestIndividualOfThisGeneration, Individual bestIndividualOfAllGenerations, bool isAlgorithmFinishedRunning)
	{
		generationText.text = currentGeneration.ToString();
		List<int> exploredByDrones = new List<int>();
		for(int i = 0; i < cellRowColumnCount; i++)
		{
			for(int j = 0; j < cellRowColumnCount; j++)
			{
				cells[i][j].droneLocationText.text = "";
				exploredByDrones.Clear();
				for(int k = 0; k < droneCount; k++)
				{
					if(bestIndividualOfThisGeneration != null && bestIndividualOfThisGeneration.drones[k].exploredCells[i, j] == true)
					{
						exploredByDrones.Add(k);
					}
				}
				cells[i][j].ExploreByDrones(exploredByDrones);
			}
		}
		if (drawDronePathsEachGeneration)
		{
			for (int i = 0; i < lines.Count; i++)
			{
				VectorLine.lineManager.DisableLine(lines[i], 0f);
			}
			lines.Clear();
			for (int i = 0; i < droneCount; i++)
			{
				List<Cell> tempPath = bestIndividualOfThisGeneration.drones[i].path;
				Vector2[] linePoints = new Vector2[tempPath.Count];
				for (int j = 0; j < tempPath.Count; j++)
				{
					linePoints[j] = (tempPath[j].pixelPosition);

				}
				lines.Add(VectorLine.SetLine(droneColors[i], 2f, linePoints));
			}
		}
		bestFitnessOfThisGenerationText.text = bestIndividualOfThisGeneration.totalWeightedFitness.ToString("F3");
		currentGenExploredCellsCountText.text = bestIndividualOfThisGeneration.totalExploredCellCount.ToString();
		bestFitnessOfAllGenerationsText.text = bestIndividualOfAllGenerations.totalWeightedFitness.ToString("F3");

		if (bestIndividualOfThisGeneration.totalExploredCellCount > maxExploredCellCount)
		{
			maxExploredCellsCountText.text = bestIndividualOfThisGeneration.totalExploredCellCount.ToString();
			maxExploredCellCount = bestIndividualOfThisGeneration.totalExploredCellCount;
		}

		for (int i = 0; i < droneCount; i++)
		{
			if (cells[bestIndividualOfThisGeneration.drones[i].lastLocation.x][bestIndividualOfThisGeneration.drones[i].lastLocation.y].droneLocationText.text != "")
			{
				cells[bestIndividualOfThisGeneration.drones[i].lastLocation.x][bestIndividualOfThisGeneration.drones[i].lastLocation.y].droneLocationText.text += "-" + (i + 1);
			}
			else
			{
				if(cells[bestIndividualOfThisGeneration.drones[i].lastLocation.x][bestIndividualOfThisGeneration.drones[i].lastLocation.y].droneLocationText.text.Length > 10)
				{
					cells[bestIndividualOfThisGeneration.drones[i].lastLocation.x][bestIndividualOfThisGeneration.drones[i].lastLocation.y].droneLocationText.text += "\n";
				}
				cells[bestIndividualOfThisGeneration.drones[i].lastLocation.x][bestIndividualOfThisGeneration.drones[i].lastLocation.y].droneLocationText.text += "Drone " + (i + 1);
			}
		}

		if (isAlgorithmFinishedRunning)
		{
			for (int i = 0; i < cellRowColumnCount; i++)
			{
				for (int j = 0; j < cellRowColumnCount; j++)
				{
					cells[i][j].droneLocationText.text = "";
					exploredByDrones.Clear();
					for (int k = 0; k < droneCount; k++)
					{
						if (bestIndividualOfAllGenerations != null && bestIndividualOfAllGenerations.drones[k].exploredCells[i, j] == true)
						{
							exploredByDrones.Add(k);
						}
					}
					cells[i][j].ExploreByDrones(exploredByDrones);
				}
			}
			if (drawDronePathsBestGeneration)
			{
				for(int i = 0; i < droneCount; i++)
				{
					List<Cell> tempPath = bestIndividualOfAllGenerations.drones[i].path;
					Vector2[] linePoints = new Vector2[tempPath.Count];
					for (int j = 0; j < tempPath.Count; j++)
					{
						linePoints[j] = (tempPath[j].pixelPosition);

					}
					lines.Add(VectorLine.SetLine(droneColors[i], 4f, linePoints));
				}
			}

			for (int i = 0; i < droneCount; i++)
			{
				if (cells[bestIndividualOfAllGenerations.drones[i].lastLocation.x][bestIndividualOfAllGenerations.drones[i].lastLocation.y].droneLocationText.text != "")
				{
					cells[bestIndividualOfAllGenerations.drones[i].lastLocation.x][bestIndividualOfAllGenerations.drones[i].lastLocation.y].droneLocationText.text += "-" + (i + 1);
				}
				else
				{
					cells[bestIndividualOfAllGenerations.drones[i].lastLocation.x][bestIndividualOfAllGenerations.drones[i].lastLocation.y].droneLocationText.text += "Drone " + (i + 1);
				}
			}
			maxExploredCellsCountText.text = maxExploredCellCount.ToString();
			currentGenExploredCellsCountText.text = bestIndividualOfAllGenerations.totalExploredCellCount.ToString();
			bestFitnessOfThisGenerationText.text = bestIndividualOfAllGenerations.totalWeightedFitness.ToString("F3");
		}
		yield return null;
	}

	private void RotateMoveDirections(int direction)
	{
		if (lastMovedDirection == direction) { return; }
		switch (direction)
		{
			case 0:
				moveDirections[0] = 1;
				moveDirections[1] = 2;
				moveDirections[2] = 4;
				moveDirections[3] = 0;
				moveDirections[4] = 7;
				moveDirections[5] = 3;
				moveDirections[6] = 5;
				moveDirections[7] = 6;
				break;
			case 1:
				moveDirections[0] = 0;
				moveDirections[1] = 1;
				moveDirections[2] = 2;
				moveDirections[3] = 3;
				moveDirections[4] = 4;
				moveDirections[5] = 5;
				moveDirections[6] = 6;
				moveDirections[7] = 7;
				break;
			case 2:
				moveDirections[0] = 3;
				moveDirections[1] = 0;
				moveDirections[2] = 1;
				moveDirections[3] = 5;
				moveDirections[4] = 2;
				moveDirections[5] = 6;
				moveDirections[6] = 7;
				moveDirections[7] = 4;
				break;
			case 3:
				moveDirections[0] = 2;
				moveDirections[1] = 4;
				moveDirections[2] = 7;
				moveDirections[3] = 1;
				moveDirections[4] = 6;
				moveDirections[5] = 0;
				moveDirections[6] = 3;
				moveDirections[7] = 5;
				break;
			case 4:
				moveDirections[0] = 5;
				moveDirections[1] = 3;
				moveDirections[2] = 0;
				moveDirections[3] = 6;
				moveDirections[4] = 1;
				moveDirections[5] = 7;
				moveDirections[6] = 4;
				moveDirections[7] = 2;
				break;
			case 5:
				moveDirections[0] = 4;
				moveDirections[1] = 7;
				moveDirections[2] = 6;
				moveDirections[3] = 2;
				moveDirections[4] = 5;
				moveDirections[5] = 1;
				moveDirections[6] = 0;
				moveDirections[7] = 3;
				break;
			case 6:
				moveDirections[0] = 7;
				moveDirections[1] = 6;
				moveDirections[2] = 5;
				moveDirections[3] = 4;
				moveDirections[4] = 3;
				moveDirections[5] = 2;
				moveDirections[6] = 1;
				moveDirections[7] = 0;
				break;
			case 7:
				moveDirections[0] = 6;
				moveDirections[1] = 5;
				moveDirections[2] = 3;
				moveDirections[3] = 7;
				moveDirections[4] = 0;
				moveDirections[5] = 4;
				moveDirections[6] = 2;
				moveDirections[7] = 1;
				break;
		}
		lastMovedDirection = direction;
	}

	private Individual GetBestIndividualOfPopulation(List<Individual> population)
	{
		List<Individual> tempPopulation = population;
		sort(tempPopulation, 0, tempPopulation.Count - 1);

		return tempPopulation[tempPopulation.Count - 1];
	}


	private IEnumerator GeneticAlgorithm(List<Individual> population)
	{
		Individual bestOfAll = new Individual(cellRowColumnCount, float.MinValue, droneCount);
		Individual bestOfThisGeneration = new Individual(cellRowColumnCount, float.MinValue, droneCount);
		bestOfThisGeneration = GetBestIndividualOfPopulation(population);
		bestOfAll = bestOfThisGeneration;
		CalculateFitness(bestOfThisGeneration, startFinishLocation, true);
		yield return StartCoroutine(UpdateUI(bestOfThisGeneration, bestOfAll, false));

		while (!ShouldStop(bestOfAll, currentGeneration))
		{
			if (algorithmState == AlgorithmState.Paused)
			{
				yield return StartCoroutine(Pause());
			}
			currentGeneration++;
			population = SelectIndividuals(population, reproduceFunctionOption);
			population = Reproduce(population);
			for (int j = 0; j < population.Count; j++)
			{
				MutateChromosomes(population[j], mutationCount);
			}
			CalculateFitnesses(population);
			RotateMoveDirections(1);
			bestOfThisGeneration = GetBestIndividualOfPopulation(population);

			CalculateFitness(bestOfThisGeneration, startFinishLocation, true);
			yield return StartCoroutine(UpdateUI(bestOfThisGeneration, bestOfAll, false));

			if (bestOfThisGeneration.totalWeightedFitness > bestOfAll.totalWeightedFitness)
			{
				bestOfAll = bestOfThisGeneration;
			}
		}
		if (algorithmState == AlgorithmState.Finished)
		{
			yield return StartCoroutine(UpdateUI(bestOfThisGeneration, bestOfAll, true));
			runButtonText.text = "RUN";
			stopButtonText.text = "RESET";
		}

		if(algorithmState == AlgorithmState.Stopped)
		{
			OnStopButton();
		}
	}

	private IEnumerator Pause()
	{
		while (algorithmState == AlgorithmState.Paused)
		{
			yield return new WaitForSeconds(0.1f);
		}
	}

	private bool ShouldStop(Individual bestOfAll, int currentGeneration)
	{
		if(algorithmState == AlgorithmState.Stopped) { return true; }
		float fitness = bestOfAll.totalWeightedFitness;
		bool isReachedCondition = true;
		if (stopIfReachedMaxGenerationCount)
		{
			isReachedCondition = currentGeneration >= maxGenerationCount;
		}
		else if (stopIfExploredAllCells)
		{
			isReachedCondition = maxExploredCellCount == (cellRowColumnCount * cellRowColumnCount);
		}
		else if (stopIfReachedDesiredFitness)
		{
			isReachedCondition = bestOfAll.totalWeightedFitness >= desiredFitness;
		}

		if (isReachedCondition)
		{
			algorithmState = AlgorithmState.Finished;
		}

		return isReachedCondition;
	}

	public void OnExitButton()
	{
		Application.Quit();
	}

	private void CalculateFitnesses(List<Individual> population)
	{
		foreach (Individual individual in population)
		{
			RotateMoveDirections(1);
			CalculateFitness(individual, startFinishLocation, false);
		}
	}

	private void CalculateFitness(Individual individual, Vector2Int initialLocation, bool updateExploredCellsUI)
	{
		float totalWeightedFitness = 0f;
		float exploredAreaFitness = 0f;
		float returnedToStartFitness = 0f;
		float moveRotateFitness = 0f;
		Vector2Int currentLocation = initialLocation;

		ResetExploredCells();
		if (updateExploredCellsUI)
		{
			individual.ResetExploredCells();
			for (int i = 0; i < droneCount; i++)
			{
				individual.drones[i].ResetExploredCells();
			}
		}

		for (int i = 0; i < droneCount; i++)
		{
			currentLocation = initialLocation;
			RotateMoveDirections(1);
			cells[currentLocation.x][currentLocation.y].isExplored = true;
			individual.ExploreCell(currentLocation.x, currentLocation.y, i, cells[currentLocation.x][currentLocation.y]);
			individual.drones[i].exploredCells[currentLocation.x, currentLocation.y] = true;
			for (int j = i * chromosomeLength; j < chromosomeLength * (i + 1); j++)
			{
				moveRotateFitness += CalculateMoveRotateFitness(currentLocation, individual.path[j]);
				if (!CanMoveToDirection(currentLocation, individual.path[j]))
				{
				}
				else
				{
					currentLocation = MoveToDirection(currentLocation, individual.path[j]);
					cells[currentLocation.x][currentLocation.y].isExplored = true;
					individual.ExploreCell(currentLocation.x, currentLocation.y, i, cells[currentLocation.x][currentLocation.y]);
					individual.drones[i].exploredCells[currentLocation.x, currentLocation.y] = true;
					RotateMoveDirections(individual.path[j]);
				}
			}
			individual.drones[i].lastLocation = currentLocation;
			int distanceToStartLocation = Mathf.Abs(initialLocation.x - currentLocation.x) > Mathf.Abs(initialLocation.y - currentLocation.y)
							? Mathf.Abs(initialLocation.x - currentLocation.x)
							: Mathf.Abs(initialLocation.y - currentLocation.y);
			returnedToStartFitness += 1 - (float)distanceToStartLocation / (cellRowColumnCount - 1);
		}

		moveRotateFitness /= cellRowColumnCount * cellRowColumnCount * 4.5f;
		returnedToStartFitness /= droneCount;



		int exploredCellCount = 0;
		foreach (List<Cell> cellRow in cells)
		{
			foreach (Cell cell in cellRow)
			{
				if (cell.isExplored)
				{
					exploredCellCount++;
				}
			}
		}
		individual.totalExploredCellCount = exploredCellCount;

		exploredAreaFitness += (float)exploredCellCount / (cellRowColumnCount * cellRowColumnCount);

		totalWeightedFitness += exploredAreaFitnessWeight * exploredAreaFitness;
		totalWeightedFitness += returnedToStartFitnessWeight * returnedToStartFitness;
		totalWeightedFitness += rotateFitnessWeight * moveRotateFitness;

		individual.totalWeightedFitness = totalWeightedFitness;
	}
	private void ResetExploredCells()
	{
		foreach (List<Cell> cellRow in cells)
		{
			foreach (Cell cell in cellRow)
			{
				cell.isExplored = false;
			}
		}
	}

	private Individual GenerateRandomChromosome(int chromosomeLength)
	{
		Individual individual = new Individual(cellRowColumnCount, 0f, droneCount);
		for (int i = 0; i < chromosomeLength; i++)
		{
			individual.path.Add(Random.Range(0, 8));
		}
		return individual;
	}

	private bool CanMoveToDirection(Vector2Int location, int direction)
	{
		if ((location.x + directionCoordinates[direction].x) < 0) { return false; }
		if ((location.x + directionCoordinates[direction].x) >= cellRowColumnCount) { return false; }
		if ((location.y + directionCoordinates[direction].y) < 0) { return false; }
		if ((location.y + directionCoordinates[direction].y) >= cellRowColumnCount) { return false; }

		return true;
	}

	private int CalculateMoveRotateFitness(Vector2Int location, int direction)
	{
		if (!CanMoveToDirection(location, direction)) { return -1000; }
		return moveRotateFitnesses[moveDirections[direction]];
	}

	private Vector2Int MoveToDirection(Vector2Int currentLocation, int direction)
	{
		return new Vector2Int(directionCoordinates[direction].x + currentLocation.x, directionCoordinates[direction].y + currentLocation.y);
	}

	private List<Individual> SelectIndividuals(List<Individual> population, int funcOption)
	{
		switch (funcOption)
		{
			case 1:
				return SelectIndividuals1(population);
				break;
			case 2:
				return SelectIndividuals2(population);
				break;
			case 3:
				return SelectIndividuals3(population);
				break;
			default:
				return null;
				break;
		}
	}

	private List<Individual> SelectIndividuals1(List<Individual> population)
	{
		List<Individual> newPopulation = new List<Individual>();

		float totalFitness = 0f;
		for (int i = 0; i < population.Count; i++)
		{
			totalFitness += population[i].totalWeightedFitness;
		}
		for (int i = population.Count - 1; i >= 0; i--)
		{
			if (Random.Range(0f, 1f) < (population[i].totalWeightedFitness / totalFitness))
			{
				newPopulation.Add(population[i]);
			}
		}

		return newPopulation;
	}

	private List<Individual> SelectIndividuals2(List<Individual> population)
	{
		List<Individual> sortedPopulation = new List<Individual>();
		List<Individual> newPopulation = new List<Individual>();
		sortedPopulation.AddRange(population);
		sort(sortedPopulation, 0, sortedPopulation.Count - 1);

		float totalFitness = 0f;
		for (int i = 0; i < population.Count; i++)
		{
			totalFitness += population[i].totalWeightedFitness;
		}

		for (int i = 0; i < population.Count; i++)
		{
			if (Random.Range(0f, 1f) < (population[i].totalWeightedFitness / totalFitness))
			{
				newPopulation.Add(population[i]);
			}
		}
		return newPopulation;
	}

	private List<Individual> SelectIndividuals3(List<Individual> population)
	{
		List<Individual> sortedPopulation = new List<Individual>();
		List<Individual> newPopulation = new List<Individual>();

		sortedPopulation.AddRange(population);
		sort(sortedPopulation, 0, sortedPopulation.Count - 1);

		for (int i = 0; i < selectCount; i++)
		{
			newPopulation.Add(sortedPopulation[sortedPopulation.Count - 1 - i]);
		}

		return newPopulation;
	}

	private List<Individual> Reproduce(List<Individual> populationToReproduce)
	{
		List<Individual> newPopulation = new List<Individual>();
		List<Individual> newPopulation2 = new List<Individual>();
		for (int i = 0; i < populationToReproduce.Count; i++)
		{
			for (int j = 0; j < populationToReproduce.Count; j++)
			{
				if (i != j)
				{
					newPopulation.Add(ReproduceChromosome(populationToReproduce[i], populationToReproduce[j]));
				}
			}
		}
		return newPopulation;
	}

	//TODO: birden fazla noktadan crossover
	private Individual ReproduceChromosome(Individual individual_1, Individual individual_2)
	{
		int newLengthChromosome_1 = Random.Range(0, 1) * chromosomeLength * droneCount;
		int newLengthChromosome_2 = Random.Range(0, 1) * (chromosomeLength * droneCount - newLengthChromosome_1);
		int newLengthChromosome_3 = individual_1.path.Count - (newLengthChromosome_1 + newLengthChromosome_2);

		Individual newIndividual = new Individual(cellRowColumnCount, 0f, droneCount);
		newIndividual.path.AddRange(individual_1.path.GetRange(0, newLengthChromosome_1));
		newIndividual.path.AddRange(individual_2.path.GetRange(newLengthChromosome_1, newLengthChromosome_2));
		newIndividual.path.AddRange(individual_1.path.GetRange(newLengthChromosome_2, newLengthChromosome_3));

		return newIndividual;
	}

	private void MutateChromosomes(Individual individual, int mutationCount)
	{
		for (int i = 0; i < mutationCount; i++)
		{
			individual.path[Random.Range(0, individual.path.Count)] = Random.Range(0, 8);
		}
	}

	public class Individual
	{
		public List<int> path;
		public float totalWeightedFitness;
		public int totalExploredCellCount;

		private int rowColumnCount;
		private int droneCount;
		public List<Drone> drones;

		public bool[,] exploredCells;

		public Individual(int rowColumnCount, float fitness, int droneCount)
		{
			path = new List<int>();
			drones = new List<Drone>();
			this.rowColumnCount = rowColumnCount;
			this.droneCount = droneCount;
			exploredCells = new bool[rowColumnCount, rowColumnCount];
			for (int i = 0; i < droneCount; i++)
			{
				drones.Add(new Drone(rowColumnCount, rowColumnCount));
			}
		}

		public void ExploreCell(int row, int column, int droneNumber, Cell exploredCell)
		{
			exploredCells[row, column] = true;
			drones[droneNumber].exploredCells[row, column] = true;
			drones[droneNumber].path.Add(exploredCell);
		}

		public void ResetExploredCells()
		{
			for(int i = 0;i < droneCount; i++)
			{
				drones[i].ResetExploredCells();
			}
		}
	}

	public class Drone
	{
		public bool[,] exploredCells;
		public Vector2Int lastLocation;

		private int rowCount;
		private int columnCount;

		public List<Cell> path;

		public Drone(int rowCount, int columnCount)
		{
			exploredCells = new bool[rowCount, columnCount];
			this.rowCount = rowCount;
			this.columnCount = columnCount;
			path = new List<Cell>();
		}
		public void ResetExploredCells()
		{
			for (int i = 0; i < rowCount; i++)
			{
				for (int j = 0; j < columnCount; j++)
				{
					exploredCells[i, j] = false;
				}
			}
			path.Clear();
		}
	}

	#region Merge Sort

	// Merges two subarrays of []arr.
	// First subarray is arr[l..m]
	// Second subarray is arr[m+1..r]
	void merge(List<Individual> arr, int l, int m, int r)
	{
		// Find sizes of two
		// subarrays to be merged
		int n1 = m - l + 1;
		int n2 = r - m;

		// Create temp arrays
		Individual[] L = new Individual[n1];
		Individual[] R = new Individual[n2];
		int i, j;

		// Copy data to temp arrays
		for (i = 0; i < n1; ++i)
			L[i] = arr[l + i];
		for (j = 0; j < n2; ++j)
			R[j] = arr[m + 1 + j];

		// Merge the temp arrays

		// Initial indexes of first
		// and second subarrays
		i = 0;
		j = 0;

		// Initial index of merged
		// subarry array
		int k = l;
		while (i < n1 && j < n2)
		{
			if (L[i].totalWeightedFitness <= R[j].totalWeightedFitness)
			{
				arr[k] = L[i];
				i++;
			}
			else
			{
				arr[k] = R[j];
				j++;
			}
			k++;
		}

		// Copy remaining elements
		// of L[] if any
		while (i < n1)
		{
			arr[k] = L[i];
			i++;
			k++;
		}

		// Copy remaining elements
		// of R[] if any
		while (j < n2)
		{
			arr[k] = R[j];
			j++;
			k++;
		}
	}

	// Main function that
	// sorts arr[l..r] using
	// merge()
	void sort(List<Individual> arr, int l, int r)
	{
		if (l < r)
		{
			// Find the middle
			// point
			int m = l + (r - l) / 2;

			// Sort first and
			// second halves
			sort(arr, l, m);
			sort(arr, m + 1, r);

			// Merge the sorted halves
			merge(arr, l, m, r);
		}
	}

	// A utility function to
	// print array of size n */
	static void printArray(List<Individual> arr)
	{
		int n = arr.Count;
		for (int i = 0; i < n; ++i)
			Debug.Log(arr[i] + " ");
		Debug.Log("");
	}
	#endregion
}