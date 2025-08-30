# TradeBull

Welcome to _TradeBull_, the fictional stock-trading API. The purpose of this task is to showcase how an API might be designed in such a way as to allow stock trading in a volatile stock market using a message queue.

The solution is separated into a number of projects, each with their own responsibilities.

In short, these are the projects and their intended functionality:

- TradeBull.Models
	- A set of simple models for use across the different layers of the project
- TradeBull.Validation
	- A library that is intended to perform model validation (through `FluentValidation`) upon requests that can be received by the API
- TradeBull.Data
	- The data layer of the application, responsible for setting up the database access (with Sqlite) and setting up the context configuration: tables, keys, indices and relationships
	- In addition to the above, it also exposes a number of models that are intended as POCOs.
- TradeBull.Messaging
	- Encapsulates the RabbitMQ connection and message exchange functionality
- TradeBull.Api
	- The main application of the solution, an ASP.NET 8 API with controllers to fetch trades for stock, user trades, and allows for new trades
	- Additionally, it has two background services, one to emulate changing stock prices throughout the day, and the other to consolidate the price changes into one snapshot for the day after the market closes
- TradeBull.Processor
	- A small console application that listening to messages on the queue and tries to execute trades in a very, very simplified version
- TradeBull.Tests
	- A project to host the unit / integration tests


## Limitations

As with any other application, there are limitations, some of which are known. A cursory glance through the code should reveal some notes and comments regarding hacks or possible improvements. However, below is a non-exhaustive list of limitations:

- Lack of tests
- Lack of separation between controller and business logic (in the API) - preferably CQRS or n-tier is used instead
- Minimal error-handling
- Current market value trades only (no stop / limit trades)
- No support for partial fills
- Exposure of DTO models outside the confines of Data project

## Quick-Start Guide

The solution can be ran from within Visual Studio (if you have RabbitMQ running separately), or you can run the docker-compose file to get everything up and running.

To run RabbitMQ **only**, you can use the following command:

```
docker run -d --name rabbit-server2 -p 5672:5672 rabbitmq:latest
```

To run the whole solution (the API, the processor, and RabbitMQ), navigate to the working directory and type:

```
docker compose up
```

This should spin up three containers, and also create a new directory called `data`, which will hold the shared database.

If you go to `http://localhost:8080/api/v1/stock/MSFT/trades` you should be able to see one trade (that is seeded in the DB)

You can also try to see your own trades at `http://localhost:8080/api/v1/stock/MSFT/my-trades` ... however if you open the page at this point it should be empty. You would need to execute a trade or two before you can see anything.

To do so, craft a request like so:

```
POST http://localhost:8080/api/v1/stock/MSFT/trade
Content-Type: application/json

{
  "Type": "Buy",
  "Condition": "Current",
  "Quantity": 50
}
```

This should issue a message onto the queue which is then handled by the processor to execute the trade. If you check the `my-trades` endpoint again, you should be able to see your trade.

You can also use `"Type": "Sell"` instead if you prefer. The data is seeded with two pending trades, both for the quantity of 50. If your request matches the seeded quantity, the trade should be completed, otherwise it will remain pending (partial fills are not supported).