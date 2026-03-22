export type AffiliateProvider = "amazon" | "booking";

export interface AffiliateOffer {
  provider: AffiliateProvider;
  emoji: string;
  headline: string;
  subtext: string;
  url: string;
}

export interface AdConfig {
  adsEnabled: boolean;
  amazonTag: string;
  bookingAid: string;
  adsensePublisherId: string;
  adsenseAdUnitId: string;
}
