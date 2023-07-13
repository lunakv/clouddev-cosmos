using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;


// C# record representing an item in the container
public record Product(
	string id,
	string categoryId,
	string categoryName,
	string name,
	int quantity,
	bool sale
);

class Program {
	public static void Main(string[] args) {
		run().Wait();
	}

	private static async Task run() {
		using CosmosClient client = new(
			accountEndpoint: Environment.GetEnvironmentVariable("COSMOS_ENDPOINT")!,
			authKeyOrResourceToken: Environment.GetEnvironmentVariable("COSMOS_KEY")!
		);

		// Database reference with creation if it does not already exist
		Database database = await client.CreateDatabaseIfNotExistsAsync(id: "testdb");

		Console.WriteLine($"New database:\t{database.Id}");

		// Container reference with creation if it does not alredy exist
		Container container = await database.CreateContainerIfNotExistsAsync(
			id: "container",
			partitionKeyPath: "/categoryId",
			throughput: 400
		);

		Console.WriteLine($"New container:\t{container.Id}");


		// Create new object and upsert (create or replace) to container
		Product newItem = new(
			id: "70b63682-b93a-4c77-aad2-65501347265f",
			categoryId: "61dba35b-4f02-45c5-b648-c6badc0cbd76",
			categoryName: "gear-surf-surfboards",
			name: "Yamba Surfboard",
			quantity: 12,
			sale: false
		);

		Product createdItem = await container.CreateItemAsync<Product>(
			item: newItem,
			partitionKey: new PartitionKey("61dba35b-4f02-45c5-b648-c6badc0cbd76")
		);

		Console.WriteLine($"Created item:\t{createdItem.id}\t[{createdItem.categoryName}]");

		// Point read item from container using the id and partitionKey
		Product readItem = await container.ReadItemAsync<Product>(
			id: "70b63682-b93a-4c77-aad2-65501347265f",
			partitionKey: new PartitionKey("61dba35b-4f02-45c5-b648-c6badc0cbd76")
		);


		// Create query using a SQL string and parameters
		var query = new QueryDefinition(
			query: "SELECT * FROM products p WHERE p.categoryId = @categoryId"
		)
			.WithParameter("@categoryId", "61dba35b-4f02-45c5-b648-c6badc0cbd76");

		using FeedIterator<Product> feed = container.GetItemQueryIterator<Product>(
			queryDefinition: query
		);

		while (feed.HasMoreResults)
		{
			FeedResponse<Product> response = await feed.ReadNextAsync();
			foreach (Product item in response)
			{
				Console.WriteLine($"Found item:\t{item.name}");
			}
		}
	}
}

