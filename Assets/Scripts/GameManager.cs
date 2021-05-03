using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
	[Space][Header("Grid Map")]
	[SerializeField] private GameObject gridObject;
	[SerializeField] private GameObject cellPrefab;
	[SerializeField] private int cellRowCount;
	[SerializeField] private int cellColumnCount;
	private GridLayoutGroup gridLayoutGroup;
	private List<List<Cell>> cells;
	
	[Space][Header("Drones")]
	[SerializeField] private int droneCount;
	[SerializeField] private GameObject startFinishLocationImagePrefab;
	[SerializeField] private Vector2Int startFinishLocation;
	private List<GameObject> droneLocationObjects;
	private List<int> initialMoveDirections = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7};
	private List<int> moveDirections = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7};
	private List<Vector2Int> directionCoordinates = new List<Vector2Int> {new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, -1),
																	new Vector2Int(0, 1), new Vector2Int(1, -1), new Vector2Int(1, 0), new Vector2Int(1, 1)};
	private List<int> moveRotateFitnesses = new List<int> { 4, 5, 4, 3, 3, 2, 1, 2 };
	//private List<Drone> drones = new List<Drone>();
	/// <directions>
	/// 0	1	2
	/// 3	X	4
	/// 5	6	7
	/// </directions>

	[Space][Header("Genetic Algorithm")]
	[SerializeField] private int maxGenerationCount;
	[SerializeField] private int populationCount;
	[SerializeField][Tooltip("Best selectCount individuals will be used to reproduce from.")] private int selectCount;
	[SerializeField] private int reproduceFunctionOption;
	[SerializeField] private int mutationCount;
	private int chromosomeLength;
	private int currentGeneration;
	private int maxExploredCellCount;

	[Space][Header("Fitness Functions")]
	[SerializeField] private float exploredAreaFitnessWeight;
	[SerializeField] private float returnedToStartFitnessWeight;
	[SerializeField] private float moveRotateFitnessWeight;

	[Space][Header("UI")]
	[SerializeField] private Text generationText;
	[SerializeField] private Text bestOfThisGenerationText;
	[SerializeField] private Text bestOfAllGenerationsText;
	[SerializeField] private Text currentGenExploredCellsCountText;
	[SerializeField] private Text maxExploredCellsCountText;
	[SerializeField] private Text timeElapsedText;


	private bool isRunning = false;
	private int lastMovedDirection = 1;
	List<List<Individual>> populations;
	private void Start()
	{
		gridLayoutGroup = gridObject.GetComponent<GridLayoutGroup>();
		cells = new List<List<Cell>>();

		for (int i = 0; i < cellRowCount; i++)
		{
			cells.Add(new List<Cell>());
			for (int j = 0; j < cellColumnCount; j++)
			{
				cells[i].Add(new Cell(false));
			}
		}

		InitGridMap();
		RotateMoveDirections(1);
	}

	public void OnRunButton()
	{
		populations = new List<List<Individual>>();
		for (int i = 0; i < droneCount; i++)
		{
			populations.Add(new List<Individual>());
		}
		maxExploredCellCount = 0;
		RotateMoveDirections(1);
		isRunning = true;
		StartCoroutine(InitFirstGeneration(populations));
		StartCoroutine(UpdateTimeElapsed());
	}

	private IEnumerator UpdateTimeElapsed()
	{
		float startingTime =  Time.time;
		int hours = 0;
		int minutes = 0;
		float seconds = 0;
		float elapse;
		float currentTime;
		while (isRunning)
		{
			yield return null;
			currentTime = Time.time;
			elapse = currentTime - startingTime;
			startingTime = currentTime;
			seconds += elapse; 
			if(seconds >= 60)
			{
				minutes++;
				seconds %= 60;
			}
			if(minutes >= 60)
			{
				hours++;
				minutes %= 60;
			}
			timeElapsedText.text = hours.ToString("D2") + ":" + minutes.ToString("D2") + ":" + seconds.ToString("00.0");
		}
		yield return null;
	}

	private void InitGridMap()
	{
		gridLayoutGroup.constraintCount = cellColumnCount;
		for (int i = 0; i < cellRowCount; i++)
		{
			for (int j = 0; j < cellColumnCount; j++)
			{
				cells[i][j].cellObject = Instantiate(cellPrefab, gridObject.transform);
				cells[i][j].isExplored = false;
			}
		}

		Instantiate(startFinishLocationImagePrefab, cells[startFinishLocation.x][startFinishLocation.y].cellObject.transform, false).GetComponent<RectTransform>().localPosition = new Vector3(0, -7, 0);
	}

	private IEnumerator InitFirstGeneration(List<List<Individual>> populations)
	{
		chromosomeLength = Mathf.CeilToInt((cellRowCount * cellColumnCount - 1) / droneCount) + 1;
		currentGeneration = 1;

		for (int i = 0; i < droneCount; i++)
		{
			for (int j = 0; j < populationCount; j++)
			{
				populations[i].Add(GenerateRandomChromosome(chromosomeLength));
			}
			CalculateFitnesses(populations[i]);
			RotateMoveDirections(1);
		}
		yield return StartCoroutine(GeneticAlgorithm(populations));
	}

	private IEnumerator UpdateUI(Individual bestOfThisGeneration, Individual bestOfAllGenerations, bool isAlgorithmFinishedRunning)
	{
		int exploredCellCount = 0;
		generationText.text = currentGeneration.ToString();
		switch (droneCount)
		{
			case 1:
				for (int i = 0; i < cellRowCount; i++)
				{
					for (int j = 0; j < cellColumnCount; j++)
					{
						cells[i][j].cellObject.transform.GetChild(3).GetComponent<Text>().text = "";
						cells[i][j].cellObject.transform.GetChild(0).gameObject.SetActive(false);
						if (bestOfThisGeneration != null && bestOfThisGeneration.exploredCells[i, j] == true)
						{
							cells[i][j].cellObject.transform.GetChild(0).gameObject.SetActive(true);
							exploredCellCount++;
						}
					}
				}
				cells[bestOfThisGeneration.lastLocation.x][bestOfThisGeneration.lastLocation.y].cellObject.transform.GetChild(3).GetComponent<Text>().text = "Drone 1";
				bestOfThisGenerationText.text = bestOfThisGeneration.totalWeightedFitness.ToString("F3");
				currentGenExploredCellsCountText.text = exploredCellCount.ToString();
				bestOfAllGenerationsText.text = bestOfAllGenerations.totalWeightedFitness.ToString("F3");
				if(exploredCellCount > maxExploredCellCount)
				{
					maxExploredCellsCountText.text = exploredCellCount.ToString();
					maxExploredCellCount = exploredCellCount;
				}
				if (isAlgorithmFinishedRunning)
				{
					exploredCellCount = 0;
					for (int i = 0; i < cellRowCount; i++)
					{
						for (int j = 0; j < cellColumnCount; j++)
						{
							cells[i][j].cellObject.transform.GetChild(3).GetComponent<Text>().text = "";
							cells[i][j].cellObject.transform.GetChild(0).gameObject.SetActive(false);
							if (bestOfAllGenerations != null && bestOfAllGenerations.exploredCells[i, j] == true)
							{
								cells[i][j].cellObject.transform.GetChild(0).gameObject.SetActive(true);
								exploredCellCount++;
							}
						}
					}
					cells[bestOfAllGenerations.lastLocation.x][bestOfAllGenerations.lastLocation.y].cellObject.transform.GetChild(3).GetComponent<Text>().text = "Drone 1";
					currentGenExploredCellsCountText.text = exploredCellCount.ToString();
				}
				break;
			case 2:
				break;
			case 4:
				break;
		}
		yield return null;
	}

	private void RotateMoveDirections(int direction)
	{
		if(lastMovedDirection == direction) { return; }
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

	private void PrintPopulations(List<List<Individual>> populations)
	{
		for (int i = 0; i < droneCount; i++)
		{
			for (int j = 0; j < populationCount; j++)
			{
				PrintChromosome(populations[i][j], "", "");
			}
		}
	}

	private Individual PrintBestIndividual(List<Individual> population)
	{
		List<Individual> tempPopulation = population;

		sort(tempPopulation, 0, tempPopulation.Count - 1);

		string prefix = "Best: ";
		string suffix = "\t Population Size: " + population.Count;
		PrintChromosome(tempPopulation[tempPopulation.Count - 1], prefix, suffix);

		string str = "";
		foreach (int i in tempPopulation[tempPopulation.Count - 1].moveRotateFitnesses)
		{
			str += i + "|";
		}
		Debug.LogWarning(lastMovedDirection + "\t\t" + str + "EAF: " + tempPopulation[tempPopulation.Count - 1].exploredAreaFitness +
						 "\t RTS: " + tempPopulation[tempPopulation.Count - 1].returnedToStartFitness + "\t MRF: " + 
						 tempPopulation[tempPopulation.Count - 1].moveRotateFitness + "\t TotalWeighted: " + 
						 tempPopulation[tempPopulation.Count - 1].totalWeightedFitness);
		return tempPopulation[tempPopulation.Count - 1];
	}

	private void PrintChromosome(Individual individual, string prefix, string suffix)
	{
		string chromosome = prefix;
		chromosome +=	"\t\tFitness: " + individual.totalWeightedFitness.ToString("F3") +
						"\t\tMRF: "		+ individual.moveRotateFitness.ToString("F3") + 
						"\t\tRTS: "		+ individual.returnedToStartFitness.ToString("F3") +
						"\t\tLength: "	+ individual.path.Count + suffix;
		chromosome += "\t\t\t";
		for (int i = 0; i < individual.path.Count; i++)
		{
			chromosome += individual.path[i] + "-";
		}
		Debug.Log(chromosome);
	}

	private IEnumerator GeneticAlgorithm(List<List<Individual>> populations)
	{
		Individual bestOfAll = new Individual(cellRowCount, cellColumnCount, float.MinValue);
		Individual bestOfThisGeneration = new Individual(cellRowCount, cellColumnCount, float.MinValue);
		/*for (int i = 0; i < droneCount; i++)
		{
			newPopulation = populations[i];
			CalculateFitnesses(newPopulation);
		}*/
		bestOfThisGeneration = PrintBestIndividual(populations[0]);
		bestOfAll = bestOfThisGeneration;
		CalculateFitness(bestOfThisGeneration, startFinishLocation, true);
		yield return StartCoroutine(UpdateUI(bestOfThisGeneration, bestOfAll, false));

		while ((bestOfAll.exploredAreaFitness < 1f) || (returnedToStartFitnessWeight < 1f))
		{
			currentGeneration++;
			for(int i = 0; i < droneCount; i++)
			{
				populations[0] = SelectIndividuals(populations[0], reproduceFunctionOption);
				populations[0] = Reproduce(populations[0]);
				for (int j = 0; j < populations[0].Count; j++)
				{
					MutateChromosomes(populations[0][j], mutationCount);
				}
				CalculateFitnesses(populations[0]);
				RotateMoveDirections(1);
				bestOfThisGeneration = PrintBestIndividual(populations[0]);

				CalculateFitness(bestOfThisGeneration, startFinishLocation, true);
				yield return StartCoroutine(UpdateUI(bestOfThisGeneration, bestOfAll, false));

				if (bestOfThisGeneration.totalWeightedFitness > bestOfAll.totalWeightedFitness)
				{
					//Debug.LogError(bestOfThisGeneration.totalWeightedFitness + "   " + bestOfAll.totalWeightedFitness);
					bestOfAll = bestOfThisGeneration;
				}
			}
		}
		PrintChromosome(bestOfAll, "BEST OF ALL: ", "");
		isRunning = false;
		yield return StartCoroutine(UpdateUI(bestOfThisGeneration, bestOfAll, true));
		yield return null;
	}

	private void CalculateFitnesses(List<Individual> population)
	{
		foreach(Individual individual in population)
		{
			RotateMoveDirections(1);
			CalculateFitness(individual, startFinishLocation, false);
		}
	}

	private void CalculateFitness(Individual individual, Vector2Int initialLocation, bool setExploredCells)
	{
		float totalWeightedFitness = 0f;
		float exploredAreaFitness = 0f;
		float returnedToStartFitness = 0f;
		float moveRotateFitness = 0f;
		Vector2Int currentLocation = initialLocation;

		ResetExploredCells();
		if (setExploredCells)
		{
			individual.ResetExploredCells(cellRowCount, cellColumnCount);
		}
		cells[currentLocation.x][currentLocation.y].isExplored = true;
		individual.exploredCells[currentLocation.x, currentLocation.y] = true;

		for(int i = 0; i < individual.path.Count; i++)
		{
			moveRotateFitness += CalculateMoveRotateFitness(currentLocation, individual.path[i]);
			individual.moveRotateFitnesses.Add(CalculateMoveRotateFitness(currentLocation, individual.path[i]));
			if(!CanMoveToDirection(currentLocation, individual.path[i])) 
			{ 
			}
			else
			{
				currentLocation = MoveToDirection(currentLocation, individual.path[i]);
				cells[currentLocation.x][currentLocation.y].isExplored = true;

				individual.exploredCells[currentLocation.x, currentLocation.y] = true;
				RotateMoveDirections(individual.path[i]);
			}
		}
		individual.lastLocation = currentLocation;

		moveRotateFitness /= cellRowCount * cellColumnCount * 5; 
		int distanceToStartLocation = Mathf.Abs(initialLocation.x - currentLocation.x) > Mathf.Abs(initialLocation.y - currentLocation.y)
									? Mathf.Abs(initialLocation.x - currentLocation.x)
									: Mathf.Abs(initialLocation.y - currentLocation.y);

		returnedToStartFitness = 1 - (float)distanceToStartLocation / (cellRowCount - 1);

		int exploredCellCount = 0;
		foreach(List<Cell> cellRow in cells)
		{
			foreach(Cell cell in cellRow)
			{
				if (cell.isExplored)
				{
					exploredCellCount++;
				} 
			}
		}

		//exploredAreaFitness = exploredCellCount;
		exploredAreaFitness += (float) exploredCellCount / (cellRowCount * cellColumnCount);

		totalWeightedFitness += exploredAreaFitnessWeight * exploredAreaFitness;
		totalWeightedFitness += returnedToStartFitnessWeight * returnedToStartFitness;
		totalWeightedFitness += moveRotateFitnessWeight * moveRotateFitness;
		individual.exploredAreaFitness = exploredAreaFitness;
		individual.returnedToStartFitness = returnedToStartFitness;
		individual.moveRotateFitness = moveRotateFitness;
		individual.totalWeightedFitness = totalWeightedFitness;
	}

	private void ResetExploredCells()
	{
		foreach(List<Cell> cellRow in cells)
		{
			foreach(Cell cell in cellRow)
			{
				cell.isExplored = false;
			}
		}
	}

	private Individual GenerateRandomChromosome(int chromosomeLength)
	{
		Individual individual = new Individual(cellRowCount, cellColumnCount, 0f);
		for(int i = 0; i < chromosomeLength; i++)
		{
			individual.path.Add(Random.Range(0, 8));
		}
		return individual;
	}

	private bool CanMoveToDirection(Vector2Int location, int direction)
	{
		if((location.x + directionCoordinates[direction].x) < 0) { return false; }
		if((location.x + directionCoordinates[direction].x) >= cellColumnCount) { return false; }
		if ((location.y + directionCoordinates[direction].y) < 0) { return false; }
		if ((location.y + directionCoordinates[direction].y) >= cellRowCount) { return false; }

		return true;
	}

	private int CalculateMoveRotateFitness(Vector2Int location, int direction)
	{
		if(!CanMoveToDirection(location, direction)) { return -1000; }
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
			if(Random.Range(0f, 1f) < (population[i].totalWeightedFitness / totalFitness))
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
				if(i != j)
				newPopulation.Add(ReproduceChromosome(populationToReproduce[i], populationToReproduce[j]));
			}
		}

		/*int remainingRequiredIndividualCount = populationCount - newPopulation.Count;
		for(int i = 0; i < remainingRequiredIndividualCount; i++)
		{
			newPopulation2.Add(newPopulation[Random.Range(0, newPopulation.Count)]);
		}
		for (int i = 0; i < remainingRequiredIndividualCount; i++)
		{
			MutateChromosomes(newPopulation2[i], 2);
		}
		newPopulation.AddRange(newPopulation2);*/
		Debug.LogError(newPopulation.Count);
		return newPopulation;
	}

/*	private List<Individual> Reproduce(List<Individual> population)
	{
		List<Individual> newPopulation = new List<Individual>();
		float crossRatio = Random.Range(0, 1);

		int firstPart = (int)(chromosomeLength * crossRatio);
		for(int i = 0; i < crossOverCount; i++)
		{
			for(int j = i + 1; j < crossOverCount; j++)
			{
				newPopulation.AddRange()
			}
		}
	}*/

	//TODO: birden fazla noktadan crossover
	private Individual ReproduceChromosome(Individual individual_1, Individual individual_2)
	{
		//int newLengthChromosome_1 = Mathf.RoundToInt(individual_1.path.Count * (individual_1.totalWeightedFitness / (individual_1.totalWeightedFitness + individual_2.totalWeightedFitness)));
		int newLengthChromosome_1 = Random.Range(0, 1) * chromosomeLength;
		int newLengthChromosome_2 = individual_1.path.Count - newLengthChromosome_1;

		Individual newIndividual = new Individual(cellRowCount, cellColumnCount, 0f);
		newIndividual.path.AddRange(individual_1.path.GetRange(0, newLengthChromosome_1));
		newIndividual.path.AddRange(individual_2.path.GetRange(newLengthChromosome_1, newLengthChromosome_2));

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

		public float exploredAreaFitness;
		public float returnedToStartFitness;
		public float moveRotateFitness;
		public List<int> moveRotateFitnesses;

		public bool[,] exploredCells;
		public Vector2Int lastLocation;

		public Individual(int rowCount, int columnCount, float fitness)
		{
			path = new List<int>();
			moveRotateFitnesses = new List<int>();
			exploredCells = new bool[rowCount, columnCount];
			this.totalWeightedFitness = fitness;
		}

		public void ResetExploredCells(int rowCount, int columnCount)
		{
			for (int i = 0; i < rowCount; i++)
			{
				for (int j = 0; j < columnCount; j++)
				{
					exploredCells[i, j] = false;
				}
			}
		}
	}

	public class Cell
	{
		public GameObject cellObject;
		public bool isExplored;

		public Cell(bool isExplored)
		{
			this.isExplored = isExplored;
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

	/*// Driver code
	public static void Main(string[] args)
	{
		int[] arr = { 12, 11, 13, 5, 6, 7 };
		Console.WriteLine("Given Array");
		printArray(arr);
		MergeSort ob = new MergeSort();
		ob.sort(arr, 0, arr.Length - 1);
		Console.WriteLine("\nSorted array");
		printArray(arr);
	}*/
	#endregion
}

