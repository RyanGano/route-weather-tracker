import type { PassWeatherForecast } from '../types/passTypes';

interface WeatherDisplayProps {
  weather: PassWeatherForecast | null;
}

function owmIconUrl(icon: string) {
  return `https://openweathermap.org/img/wn/${icon}@2x.png`;
}

function formatTemp(f: number) {
  return `${Math.round(f)}°F`;
}

export default function WeatherDisplay({ weather }: WeatherDisplayProps) {
  if (!weather) {
    return (
      <div className="text-muted small fst-italic py-2">Weather data unavailable</div>
    );
  }

  return (
    <div>
      {/* Current conditions */}
      <div className="d-flex align-items-center gap-2 mb-2">
        {weather.currentIconCode && (
          <img
            src={owmIconUrl(weather.currentIconCode)}
            alt={weather.currentDescription}
            width={40}
            height={40}
          />
        )}
        <div>
          <span className="fs-5 fw-bold">{formatTemp(weather.currentTempFahrenheit)}</span>
          <span className="text-muted ms-2 text-capitalize">{weather.currentDescription}</span>
        </div>
      </div>

      {/* 5-day forecast */}
      {weather.dailyForecasts.length > 0 && (
        <div className="d-flex gap-2 flex-wrap">
          {weather.dailyForecasts.map((day) => {
            const date = new Date(day.date);
            const label = date.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });
            return (
              <div
                key={day.date}
                className="text-center border rounded p-1"
                style={{ minWidth: '70px', fontSize: '0.75rem' }}
              >
                <div className="fw-semibold text-muted">{label}</div>
                {day.iconCode && (
                  <img src={owmIconUrl(day.iconCode)} alt={day.description} width={32} height={32} />
                )}
                <div>
                  <span className="fw-bold">{formatTemp(day.highFahrenheit)}</span>
                  <span className="text-muted"> / {formatTemp(day.lowFahrenheit)}</span>
                </div>
                {day.precipitationMm > 0 && (
                  <div className="text-info">&#128166; {day.precipitationMm.toFixed(1)} mm</div>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
