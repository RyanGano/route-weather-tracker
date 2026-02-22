export const TravelRestriction = {
  None: 0,
  TiresOrTraction: 1,
  ChainsRequired: 2,
  Closed: 3,
} as const;
export type TravelRestriction =
  (typeof TravelRestriction)[keyof typeof TravelRestriction];

export interface PassInfo {
  id: string;
  name: string;
  highway: string;
  elevationFeet: number;
  latitude: number;
  longitude: number;
  state: string;
  officialUrl: string;
}

export interface PassCondition {
  passId: string;
  roadCondition: string;
  weatherCondition: string;
  eastboundRestriction: TravelRestriction;
  westboundRestriction: TravelRestriction;
  /** Raw restriction text from the data source (e.g. "Traction Tires Advised, Oversize Vehicles Prohibited"). Empty for derived conditions. */
  eastboundRestrictionText: string;
  westboundRestrictionText: string;
  temperatureFahrenheit: number;
  lastUpdated: string;
}

export interface CameraImage {
  cameraId: string;
  description: string;
  imageUrl: string;
  fetchedAt: string;
}

export interface WeatherForecastDay {
  date: string;
  highFahrenheit: number;
  lowFahrenheit: number;
  description: string;
  iconCode: string;
  precipitationMm: number;
  windSpeedMph: number;
}

export interface PassWeatherForecast {
  passId: string;
  currentTempFahrenheit: number;
  currentDescription: string;
  currentIconCode: string;
  dailyForecasts: WeatherForecastDay[];
}

export interface PassSummary {
  info: PassInfo;
  condition: PassCondition | null;
  cameras: CameraImage[];
  weather: PassWeatherForecast | null;
}
