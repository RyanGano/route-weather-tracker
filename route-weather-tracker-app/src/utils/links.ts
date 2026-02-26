export function nwsForecastUrl(lat: number, lon: number): string {
  // MapClick link on forecast.weather.gov provides a point forecast page
  return `https://forecast.weather.gov/MapClick.php?lat=${lat}&lon=${lon}`;
}
