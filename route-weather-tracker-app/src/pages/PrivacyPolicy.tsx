import Container from "react-bootstrap/Container";
import { Link } from "react-router-dom";

const OWNER = "Ryan Gano";
const CONTACT_EMAIL = "ryan.junkmail.77@gmail.com";
const SITE_NAME = "When to Drive";
const LAST_UPDATED = "March 21, 2026";

export default function PrivacyPolicy() {
  return (
    <Container className="py-5" style={{ maxWidth: "720px" }}>
      <h1 className="mb-1">{SITE_NAME} — Privacy Policy</h1>
      <p className="text-muted small mb-4">Last updated: {LAST_UPDATED}</p>

      <p>
        This is a straightforward privacy policy written in plain language. If
        you have questions, email{" "}
        <a href={`mailto:${CONTACT_EMAIL}`}>{CONTACT_EMAIL}</a> and a real
        person will respond.
      </p>

      <hr />

      <h2 className="h5 mt-4">Who runs this site?</h2>
      <p>
        {SITE_NAME} is operated by {OWNER}, an individual developer in the
        United States.
      </p>

      <h2 className="h5 mt-4">What data do we collect?</h2>
      <p>
        <strong>Almost nothing directly.</strong> We do not ask you to create an
        account, and we do not collect your name, address, or payment
        information.
      </p>
      <p>
        When you use the site, the following happens automatically — this is
        true of essentially every website:
      </p>
      <ul>
        <li>
          Your browser sends a request to our servers to load the page and fetch
          pass/weather data. Our server logs record your IP address, browser
          type, and which pages you requested. These logs are used only for
          debugging and security, and are not sold or shared.
        </li>
        <li>
          If you allow your browser to share your location (so we can sort city
          suggestions by distance), we receive your approximate coordinates
          temporarily in the browser.{" "}
          <strong>This is never sent to our servers and never stored.</strong>
        </li>
      </ul>

      <h2 className="h5 mt-4">Advertising and affiliate links</h2>
      <p>
        This site displays travel-relevant recommendations — such as links to
        tire chains or nearby hotels — that may be affiliate links or
        advertisements. Here is how each service works:
      </p>

      <h3 className="h6 mt-3">Amazon Associates</h3>
      <p>
        Some links on this site point to Amazon.com and include a tracking tag.
        <strong>
          {" "}
          As an Amazon Associate I earn from qualifying purchases.
        </strong>{" "}
        When you click an Amazon link and buy something, Amazon pays us a small
        commission. Amazon may set cookies in your browser for purchase
        attribution. Amazon's privacy policy is available at{" "}
        <a
          href="https://www.amazon.com/gp/help/customer/display.html?nodeId=468496"
          target="_blank"
          rel="noopener noreferrer"
        >
          amazon.com/privacy
        </a>
        .
      </p>

      <h3 className="h6 mt-3">Booking.com</h3>
      <p>
        Some links point to Booking.com hotel search results. If you make a
        booking after clicking one of these links, Booking.com pays us a
        referral commission. Booking.com's privacy policy is at{" "}
        <a
          href="https://www.booking.com/content/privacy.html"
          target="_blank"
          rel="noopener noreferrer"
        >
          booking.com/privacy
        </a>
        .
      </p>

      <h3 className="h6 mt-3">Google AdSense</h3>
      <p>
        When no specific affiliate recommendation is available, the site may
        display a Google AdSense advertisement. Google uses cookies to show ads
        based on your browsing history. You can opt out of personalized ads at{" "}
        <a
          href="https://adssettings.google.com"
          target="_blank"
          rel="noopener noreferrer"
        >
          adssettings.google.com
        </a>
        , or turn off interest-based advertising through the{" "}
        <a
          href="https://optout.aboutads.info"
          target="_blank"
          rel="noopener noreferrer"
        >
          Digital Advertising Alliance opt-out tool
        </a>
        . Google's privacy policy is at{" "}
        <a
          href="https://policies.google.com/privacy"
          target="_blank"
          rel="noopener noreferrer"
        >
          policies.google.com/privacy
        </a>
        .
      </p>

      <h2 className="h5 mt-4">Do we use cookies ourselves?</h2>
      <p>
        We do not set any first-party cookies. We use{" "}
        <code>sessionStorage</code> (a browser-only, never-transmitted store) to
        remember if you have dismissed an ad card during your current session.
        This clears automatically when you close your browser tab.
      </p>

      <h2 className="h5 mt-4">Do we sell your data?</h2>
      <p>
        No. We do not sell, rent, or trade personal information to any third
        party.
      </p>

      <h2 className="h5 mt-4">California residents (CCPA)</h2>
      <p>
        If you are a California resident, you have the right to know what
        personal information we have collected about you and to request its
        deletion. Since we collect only server logs (IP addresses), you can
        request deletion of any logs associated with your IP address by emailing{" "}
        <a href={`mailto:${CONTACT_EMAIL}`}>{CONTACT_EMAIL}</a>.
      </p>

      <h2 className="h5 mt-4">Children</h2>
      <p>
        This site is not directed at children under 13, and we do not knowingly
        collect information from them.
      </p>

      <h2 className="h5 mt-4">Changes to this policy</h2>
      <p>
        If we make material changes, we will update the "Last updated" date at
        the top of this page. Continued use of the site after changes are posted
        means you accept the updated policy.
      </p>

      <h2 className="h5 mt-4">Contact</h2>
      <p>
        Questions, data requests, or concerns:{" "}
        <a href={`mailto:${CONTACT_EMAIL}`}>{CONTACT_EMAIL}</a>
      </p>

      <hr className="mt-5" />
      <p className="text-muted small">
        <Link to="/">← Back to {SITE_NAME}</Link>
      </p>
    </Container>
  );
}
