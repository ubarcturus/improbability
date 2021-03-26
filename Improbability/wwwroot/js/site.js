// @ts-nocheck
// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const showStatisticButton = document.querySelector('.show-statistic');
showStatisticButton.addEventListener('click', showStatistic);

async function showStatistic() {
	const [apiKey, randomItemId, token] = readInputs();
	const body = await getRandomEvents(apiKey, randomItemId, token);
	drawHistogram(body);
	drawScatterChart(body);
	showStatistics(body);
}

function readInputs() {
	const apiKey = document.querySelector('.apiKey').value;
	const randomItemId = document.querySelector('.randomItemId').value;
	const token = document.querySelector(
		'input[name="__RequestVerificationToken"]'
	).value;

	return [apiKey, randomItemId, token];
}

async function getRandomEvents(apiKey, randomItemId, token) {
	const url = `/randomItemDiagrams?handler=GetFromApi`;
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
	const body = await response.json();

	return body;
}

google.charts.load('current', { packages: ['corechart', 'scatter'] });
// Google.charts.setOnLoadCallback(drawChart);

function drawHistogram(body) {
	const results = body.randomEvents.map((randomEvent) => {
		return [`Id: ${randomEvent.id.toString()}`, randomEvent.result];
	});
	results.unshift(['Id', 'Result']);
	const data = google.visualization.arrayToDataTable(results);

	const options = {
		title: `Histogram of events of item: ${body.randomItem.name}`,
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

function drawScatterChart(body) {
	const results = body.randomEvents.map((randomEvent) => {
		return [`Event ID: ${randomEvent.id}`, randomEvent.result];
	});
	results.unshift(['Event ID', 'Result']);
	const data = google.visualization.arrayToDataTable(results);

	const options = {
		title: `Events results of item: ${body.randomItem.name}`,
		hAxis: { title: 'Event ID' },
		vAxis: { title: 'Result' },
	};

	const chart = new google.visualization.ScatterChart(
		document.querySelector('#scatterChart_div')
	);
	chart.draw(data, options);
}

function showStatistics(body) {
	document.querySelector('#minimum').textContent = calcMinimum(body);
	document.querySelector('#maximum').textContent = calcMaximum(body);
	document.querySelector('#average').textContent = calcAverage(body);
}

function calcMinimum(body) {
	const min = body.randomEvents
		.flatMap((randomEvent) => randomEvent.result)
		.sort((a, b) => a - b)[0];
	return min;
}

function calcMaximum(body) {
	const max = body.randomEvents
		.flatMap((randomEvent) => randomEvent.result)
		.sort((a, b) => b - a)[0];
	return max;
}

function calcAverage(body) {
	const sum = body.randomEvents.reduce(
		(acc, randomEvent) => acc + randomEvent.result,
		0
	);
	const avg = sum / body.randomEvents.length;
	return avg;
}
