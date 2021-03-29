// @ts-nocheck
// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const showStatisticButton = document.querySelector('#show-statistic');
showStatisticButton.addEventListener('click', showStatistic);

async function showStatistic() {
	const [apiKey, randomItemId, token] = readInputs();
	const randomEvents = await getRandomEvents(apiKey, randomItemId, token);
	drawHistogram(randomEvents);
	drawScatterChart(randomEvents);
	calcStatistics(randomEvents);
}

function readInputs() {
	const apiKey = document.querySelector('#apiKey').value;
	const randomItemId = document.querySelector('#randomItemId').value;
	const token = document.querySelector(
		'input[name="__RequestVerificationToken"]'
	).value;

	return [apiKey, randomItemId, token];
}

async function getRandomEvents(apiKey, randomItemId, token) {
	const url = '/randomItemDiagrams?handler=GetFromApi';
	const response = await fetch(url, {
		method: 'POST',
		headers: {
			RequestVerificationToken: token,
			'Content-Type': 'application/json',
		},
		body: JSON.stringify({
			apiKey,
			randomItemId,
		}),
	});
	const randomEvents = await response.json();

	return randomEvents;
}

google.charts.load('current', { packages: ['corechart', 'scatter'] });

function drawHistogram(randomEvents) {
	const results = randomEvents.randomEvents.map((randomEvent) => [
		`Id: ${randomEvent.id.toString()}`,
		randomEvent.result,
	]);
	results.unshift(['Id', 'Result']);
	const data = google.visualization.arrayToDataTable(results);

	const options = {
		title: `Histogram of events of item: ${randomEvents.randomItem.name}`,
		// Legend: { position: 'in' },
		// HAxis: { title: 'Result' },
		vAxis: { title: 'Frequency' },
		histogram: { bucketSize: 1 },
	};

	const chart = new google.visualization.Histogram(
		document.querySelector('#histogram_div')
	);
	chart.draw(data, options);
}

function drawScatterChart(randomEvents) {
	const results = randomEvents.randomEvents.map((randomEvent) => {
		return [`Event ID: ${randomEvent.id}`, randomEvent.result];
	});
	results.unshift(['Event ID', 'Result']);
	const data = google.visualization.arrayToDataTable(results);

	const options = {
		title: `Events results of item: ${randomEvents.randomItem.name}`,
		hAxis: { title: 'Event ID' },
		vAxis: { title: 'Result' },
	};

	const chart = new google.visualization.ScatterChart(
		document.querySelector('#scatterChart_div')
	);
	chart.draw(data, options);
}

function calcStatistics(randomEvents) {
	document.querySelector('#minimum').textContent = calcMinimum(randomEvents);
	document.querySelector('#maximum').textContent = calcMaximum(randomEvents);
	document.querySelector('#average').textContent = calcAverage(randomEvents);

	document.querySelector(
		'#expected-avg-start0'
	).textContent = calcExpectedAvgStart0(randomEvents);

	document.querySelector(
		'#expected-avg-start1'
	).textContent = calcExpectedAvgStart1(randomEvents);

	document.querySelector('#median').textContent = calcMedian(randomEvents);

	document.querySelector('#standard-deviation').textContent = calcStdDev(
		randomEvents
	);
}

function calcMinimum(randomEvents) {
	return randomEvents.randomEvents
		.flatMap((randomEvent) => randomEvent.result)
		.sort((a, b) => a - b)[0];
}

function calcMaximum(randomEvents) {
	return randomEvents.randomEvents
		.flatMap((randomEvent) => randomEvent.result)
		.sort((a, b) => b - a)[0];
}

function calcAverage(randomEvents) {
	const sum = randomEvents.randomEvents.reduce(
		(acc, randomEvent) => acc + randomEvent.result,
		0
	);
	return sum / randomEvents.randomEvents.length;
}

function calcExpectedAvgStart0(randomEvents) {
	return randomEvents.randomItem.numberOfPossibleResults / 2;
}

function calcExpectedAvgStart1(randomEvents) {
	return (randomEvents.randomItem.numberOfPossibleResults + 1) / 2;
}

function calcMedian(randomEvents) {
	const sortedResults = randomEvents.randomEvents
		.flatMap((randomEvent) => randomEvent.result)
		.sort((a, b) => a - b);
	const middle = Math.floor(sortedResults.length / 2);

	if (sortedResults.length % 2 === 0) {
		return (sortedResults[middle - 1] + sortedResults[middle]) / 2;
	}

	return sortedResults[middle];
}

function calcStdDev(randomEvents) {
	const avg = calcAverage(randomEvents);
	const sum = randomEvents.randomEvents.reduce(
		(acc, randomEvent) => acc + (randomEvent.result - avg) ** 2,
		0
	);
	return Math.sqrt(sum / randomEvents.randomEvents.length);
}
