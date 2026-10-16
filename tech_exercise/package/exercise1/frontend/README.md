# ACTS Frontend

Angular interface for the Stargate API in `../api`.

## Run locally

Start the API in Development mode first. Its HTTP profile listens on `http://localhost:5204`.

```powershell
npm install
npm start
```

Open `http://localhost:4200`. The Angular development server proxies `/Person` and `/AstronautDuty` requests to the API.

## Available operations

- Browse and search people, and view astronaut duty history.
- Create a person and rename an existing person.
- Add a duty assignment. The API automatically closes the previous duty.