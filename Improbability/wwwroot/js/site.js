// @ts-nocheck
// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

'use strict';

const showStatisticButton = document.querySelector('#show-statistic');
showStatisticButton.addEventListener('click', showStatistic);

async function showStatistic() {
	const [apiKey, randomItemId, token] = readInputs();
	const randomEvents = await getRandomEvents(apiKey, randomItemId, token);
	console.log(calcChiSquared(randomEvents));
	console.log(calcChiSquaredBiased(randomEvents, 950));
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

/*
https://en.wikipedia.org/wiki/Pearson%27s_chi-squared_test
*/
function calcChiSquared(randomEvents) {
	const numberOfPossibleResults =
		randomEvents.randomItem.numberOfPossibleResults;
	const histogram = {};
	for (let i = 1; i <= numberOfPossibleResults; i++) {
		histogram[i] = 0;
	}

	for (const randomEvent of randomEvents.randomEvents) {
		histogram[randomEvent.result] += 1;
	}

	let sum = 0;
	const expected = randomEvents.randomEvents.length / numberOfPossibleResults;
	for (let i = 1; i <= numberOfPossibleResults; i++) {
		sum += (histogram[i] - expected) ** 2 / expected;
	}

	return sum;
}

function calcChiSquaredDegreeOfFreedom(randomEvents) {
	const numberOfPossibleResults =
		randomEvents.randomItem.numberOfPossibleResults;
	return numberOfPossibleResults - 1;
}

const chiSquaredDistribution = {
	900: [],
	950: [3.84, 5.99, 7.81, 9.49, 11.1, 12.6, 14.1],
	990: [],
	995: [],
	// TODO continue list
};

/*
True = random events may be biased
false = no answer possible
*/
function calcChiSquaredBiased(randomEvents, significance) {
	const chiSquared = calcChiSquared(randomEvents);
	const degreeOfFreedom = calcChiSquaredDegreeOfFreedom(randomEvents);
	return chiSquared > chiSquaredDistribution[significance][degreeOfFreedom - 1];
}

/*
Old
const bodyResults = [ 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6 ];
const body = { randomEvents: bodyResults.map(($) => ({ result: $ })) };

const randomEvents = {
	randomItem: {
		id: 0,
		name: '',
		numberOfPossibleResults: 0,
		description: '',
	},
	randomEvents: [
		{
			id: 0,
			name: '',
			time: '',
			result: 0,
			description: '',
			randomItemId: 0,
		},
	],
};
*/
