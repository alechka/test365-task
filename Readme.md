# Distributed scores 
this project uses multiple workers (ScoreService) to store  data for game scores. Duplicates are handled
with reddis cache. 

To get list of scores scatter-gather pattern is used.

## Projects:
- Test365.AppHost - DotNet Aspire start up project (runs and services and infrastructure)
- Test365.ApiService - REST service to handle list requests. Also makes calls to Test365.ScoreService
- Test365.ScoreService - Microservice to stores scores and reply to list requests
- Test365.PublisherConsole - Publishes random scores messages to RabbitMQ 
- Test365.Common - Shared constants and contracts 