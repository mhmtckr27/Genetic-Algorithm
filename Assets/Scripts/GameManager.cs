using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
	[Header("Grid Map")]
	[SerializeField] private GameObject gridObject;
	[SerializeField] private GameObject cellPrefab;
	[SerializeField] private int cellRowCount;
	[SerializeField] private int cellColumnCount;
	private GridLayoutGroup gridLayoutGroup;
	private List<List<GameObject>> cells;
	[Space]

	[Header("Drones")]
	[SerializeField] private int droneCount;
	[SerializeField] private Vector2 startFinishPoint;
	private List<int> moveDirections = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7};
	private List<Vector2> directionCoordinates = new List<Vector2> {new Vector2(-1, -1), new Vector2(-1, 0), new Vector2(-1, 1), new Vector2(0, -1),
																	new Vector2(0, 1), new Vector2(1, -1), new Vector2(1, 0), new Vector2(1, 1)};
	private List<int> directionCosts = new List<int> { 2, 1, 2, 3, 3, 4, 5, 4 };
	/// <directions>
	/// 0	1	2
	/// 3	X	4
	/// 5	6	7
	/// </directions>
	private float mutationProbability;
	private List<List<bool>> exploredAreas;
	[Space]

	[Header("Genetic Algorithm")]
	[SerializeField] private int populationCount;
	private List<List<Individual>> populations;


	private void Start()
	{
		gridLayoutGroup = gridObject.GetComponent<GridLayoutGroup>();
		cells = new List<List<GameObject>>();
		exploredAreas = new List<List<bool>>();
		populations = new List<List<Individual>>();

		for(int i = 0; i < cellRowCount; i++)
		{
			cells.Add(new List<GameObject>());
			exploredAreas.Add(new List<bool>());
		}
		for(int i = 0; i < droneCount; i++)
		{
			populations.Add(new List<Individual>());
		}

		InitGridMap();
		InitFirstGeneration();
	}

	private void InitGridMap()
	{
		gridLayoutGroup.constraintCount = cellColumnCount;
		for(int i = 0; i < cellRowCount; i++)
		{
			for(int j = 0; j < cellColumnCount; j++)
			{
				cells[i].Add(Instantiate(cellPrefab, gridObject.transform));
			}
		}
	}

	private void InitFirstGeneration()
	{
		int chromosomeLength = Mathf.CeilToInt(cellRowCount * cellColumnCount - 1);

		for(int i = 0; i < droneCount; i++)
		{
			for(int j = 0; j < populationCount; j++)
			{
				populations[i].Add(GenerateRandomChromosome(chromosomeLength));
				PrintChromosome(populations[i][j]);
			}
		}
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
		chromosome += "\t\tLength: " + individual.path.Count;
		Debug.Log(chromosome);
	}

	private bool CanMoveToDirection(Vector2 location, int direction)
	{
		if((location.x + directionCoordinates[direction].x) < 0) { return false; }
		if((location.x + directionCoordinates[direction].x) >= cellColumnCount) { return false; }
		if ((location.y + directionCoordinates[direction].y) < 0) { return false; }
		if ((location.y + directionCoordinates[direction].y) >= cellRowCount) { return false; }

		return true;
	}

	private int CalculateMovementCost(Vector2 location, int direction)
	{
		if(!CanMoveToDirection(location, direction)) { return 1000; }

		return directionCosts[direction];
	}

	private void SelectIndividuals(List<Individual> population, int selectCount)
	{
		sort(population, 0, population.Count - 1);
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

	private void MutateChromosome(List<int> chromosome, int mutationCount)
	{
		for (int i = 0; i < mutationCount; i++)
		{
			chromosome[Random.Range(0, chromosome.Count)] = Random.Range(0, 8);
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

