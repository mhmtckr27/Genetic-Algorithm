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
	[SerializeField] private Vector2Int startFinishLocation;
	private List<int> moveDirections = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7};
	private List<Vector2Int> directionCoordinates = new List<Vector2Int> {new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, -1),
																	new Vector2Int(0, 1), new Vector2Int(1, -1), new Vector2Int(1, 0), new Vector2Int(1, 1)};
	private List<int> moveRotateFitnesses = new List<int> { 4, 5, 4, 3, 3, 2, 1, 2 };
	/// <directions>
	/// 0	1	2
	/// 3	X	4
	/// 5	6	7
	/// </directions>
	private List<List<bool>> exploredAreas;
	
	[Space][Header("Genetic Algorithm")]
	[SerializeField] private int populationCount;
	[SerializeField][Tooltip("Best reproduceCount individuals will be used to reproduce from.")] private int reproduceCount;
	[SerializeField] private float mutationProbability;
	[SerializeField] private int mutationCount;
	private int chromosomeLength;

	[Space]
	[Header("Fitness Functions")]
	[SerializeField] private float exploredAreaWeight;
	[SerializeField] private float returnedToStartWeight;
	[SerializeField] private float moveRotateCostWeight;

	private void Start()
	{
		gridLayoutGroup = gridObject.GetComponent<GridLayoutGroup>();
		cells = new List<List<Cell>>();
		exploredAreas = new List<List<bool>>();
		List<List<Individual>> populations = new List<List<Individual>>();
		for (int i = 0; i < droneCount; i++)
		{
			populations.Add(new List<Individual>());
		}

		for (int i = 0; i < cellRowCount; i++)
		{
			cells.Add(new List<Cell>());
			for(int j = 0; j < cellColumnCount; j++)
			{
				cells[i].Add(new Cell(false));
			}
			exploredAreas.Add(new List<bool>());
		}

		InitGridMap();
		InitFirstGeneration(populations);
		PrintPopulations(populations);
		GeneticAlgorithm(populations);
		Reproduce(populations[0]);
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
	}

	private void InitFirstGeneration(List<List<Individual>> populations)
	{
		chromosomeLength = Mathf.CeilToInt((cellRowCount * cellColumnCount - 1) / droneCount) + 1;

		for (int i = 0; i < droneCount; i++)
		{
			for (int j = 0; j < populationCount; j++)
			{
				populations[i].Add(GenerateRandomChromosome(chromosomeLength));
			}
			CalculateFitnesses(populations[i]);
		}
	}

	private void PrintPopulations(List<List<Individual>> populations)
	{
		for (int i = 0; i < droneCount; i++)
		{
			for (int j = 0; j < populationCount; j++)
			{
				PrintChromosome(populations[i][j]);
			}
		}
	}

	private void GeneticAlgorithm(List<List<Individual>> populations)
	{
		//while (true)
		{
			for(int i = 0; i < droneCount; i++)
			{

				List<Individual> newPopulation = SelectIndividuals(populations[i], reproduceCount);
				newPopulation = Reproduce(newPopulation);
				for (int j = 0; j < newPopulation.Count; j++)
				{
					if(Random.Range(0, 1) < mutationProbability)
					{
						MutateChromosomes(newPopulation[j]);
					}
				}
			}
		}
	}

	private void CalculateFitnesses(List<Individual> population)
	{
		foreach(Individual individual in population)
		{
			CalculateFitness(individual, startFinishLocation);
		}
	}

	private void CalculateFitness(Individual individual, Vector2Int initialLocation)
	{
		float totalWeightedFitness = 0f;
		float exploredAreaFitness = 0f;
		float returnedToStartFitness = 0f;
		float moveRotateCost = 0f;
		Vector2Int currentLocation = initialLocation;
		cells[currentLocation.x][currentLocation.y].isExplored = true;
		
		for(int i = 0; i < individual.path.Count; i++)
		{
			if(!CanMoveToDirection(currentLocation, individual.path[i])) { continue; }
			moveRotateCost += CalculateMoveRotateCost(currentLocation, individual.path[i]);
			currentLocation = MoveToDirection(currentLocation, individual.path[i]);
		//	Debug.LogError(currentLocation);
			cells[currentLocation.x][currentLocation.y].isExplored = true;
		}

		int distanceToStartLocation = Mathf.Abs(initialLocation.x - currentLocation.x) > Mathf.Abs(initialLocation.y - currentLocation.y)
									? Mathf.Abs(initialLocation.x - currentLocation.x)
									: Mathf.Abs(initialLocation.y - currentLocation.y);

		returnedToStartFitness = -distanceToStartLocation;

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

		exploredAreaFitness = (float) exploredCellCount / (cellRowCount * cellColumnCount);

		totalWeightedFitness += exploredAreaWeight * exploredAreaFitness;
		totalWeightedFitness += returnedToStartWeight * returnedToStartFitness;
		totalWeightedFitness -= moveRotateCostWeight * moveRotateCost;

		individual.fitness = totalWeightedFitness;
	}

	private Individual GenerateRandomChromosome(int chromosomeLength)
	{
		Individual individual = new Individual(0f);
		for(int i = 0; i < chromosomeLength; i++)
		{
			individual.path.Add(Random.Range(0, 8));
		}
		return individual;
	}

	private void PrintChromosome(Individual individual)
	{
		string chromosome = "";
		for(int i = 0; i < individual.path.Count; i++)
		{
			chromosome += individual.path[i];
		}
		chromosome += "\t\tFitness: " + individual.fitness + "\t\tLength: " + individual.path.Count;
		Debug.Log(chromosome);
	}

	private bool CanMoveToDirection(Vector2Int location, int direction)
	{
		if((location.x + directionCoordinates[direction].x) < 0) { return false; }
		if((location.x + directionCoordinates[direction].x) >= cellColumnCount) { return false; }
		if ((location.y + directionCoordinates[direction].y) < 0) { return false; }
		if ((location.y + directionCoordinates[direction].y) >= cellRowCount) { return false; }

		return true;
	}

	private float CalculateMoveRotateCost(Vector2Int location, int direction)
	{
		if(!CanMoveToDirection(location, direction)) { return 1000; }

		return moveRotateFitnesses[direction];
	}

	private Vector2Int MoveToDirection(Vector2Int currentLocation, int direction)
	{
		return new Vector2Int(directionCoordinates[direction].x + currentLocation.x, directionCoordinates[direction].y + currentLocation.y);
	}

	private List<Individual> SelectIndividuals(List<Individual> population, int selectCount)
	{
		List<Individual> sortedPopulation = new List<Individual>();
		sortedPopulation.AddRange(population);
		sort(sortedPopulation, 0, sortedPopulation.Count - 1);

		return sortedPopulation.GetRange(0, selectCount);
	}

	private List<Individual> Reproduce(List<Individual> populationToReproduce)
	{
		List<Individual> newPopulation = new List<Individual>();
		for(int i = 0; i < populationToReproduce.Count; i++)
		{
			for(int j = 0; j < populationToReproduce.Count; j++)
			{
				newPopulation.Add(ReproduceChromosome(populationToReproduce[i], populationToReproduce[j]));
			}
		}
		return newPopulation;
	}

	//TODO: birden fazla noktadan crossover
	private Individual ReproduceChromosome(Individual individual_1, Individual individual_2)
	{
		int newLengthChromosome_1 = Mathf.RoundToInt(individual_1.path.Count * (individual_1.fitness / (individual_1.fitness + individual_2.fitness)));
		int newLengthChromosome_2 = individual_1.path.Count - newLengthChromosome_1;

		Individual newIndividual = new Individual(0f);
		newIndividual.path.AddRange(individual_1.path.GetRange(0, newLengthChromosome_1));
		newIndividual.path.AddRange(individual_2.path.GetRange(newLengthChromosome_1 - 1, newLengthChromosome_2));

		return newIndividual;
	}

	private void MutateChromosomes(Individual individual)
	{
		for (int i = 0; i < mutationCount; i++)
		{
			individual.path[Random.Range(0, individual.path.Count)] = Random.Range(0, 8);
		}
	}

	public struct Individual
	{
		public List<int> path;
		public float fitness;

		public Individual(float fitness)
		{
			path = new List<int>();
			this.fitness = fitness;
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


	///----------------MERGE SORT-------------------------------------

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
			if (L[i].fitness <= R[j].fitness)
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
}

