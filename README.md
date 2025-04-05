# LaunchDarkly Demo using a simple 3 tier web app (.NET 8, React)

This repo was forked from https://github.com/NetCoreTemplates/react-spa to demonstrate some key features of LaunchDarkly:
- Feature Flags: Add a flag to turn a new feature on or off. You can release the feature by turning the flag on, and roll it back by turning it off.
- Instant Updates: Set up a listener so changes to the flag take effect right away—no page refresh needed.
- Remediate: Use a trigger (like a curl command or a browser action) to quickly turn off a feature if something goes wrong.

# Prerequisites
- [.NET 8 SDK installed](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Node version 20+
- Sign up for a free LaunchDarkly account at [https://app.launchdarkly.com/signup](https://app.launchdarkly.com/signup)

# Run project locally
```bash
# install npm dependencies
cd ../MyApp.Client
npm install

# run the UI
cd MyApp
dotnet restore

# run migration and seed data
dotnet run --AppTasks=migrate

# run the app
dotnet run

# open https://localhost:5173 to see the current version of the web app as is
```

# Release and Remediate

## Feature Flag

Let's assume that the `Counter` is a brand new feature that we would like to safely release to production by toggling the feature on and rolling it back by toggling it off.

### Steps

First, you need to get your Test Environment Client Side ID by going to https://app.launchdarkly.com/projects/default/settings/environments and click on 3 dots menu and select `Client-side ID` to copy it

Go to https://app.launchdarkly.com/projects/default/flags and create a new Flag:
- name: `Feature: Counter`
- Description: `This flag control the visibility of the Counter feature on the home screen`
- Enable `SDKs using Client-side ID`
- Category: `Release`
- Type: `Boolean`
- Variations: `Available` and `Unavaiable`
- Default variations: `Unavailable` for both Target ON and OFF


```bash
# install LaunchDarkly SDK from inside \MyApp.Client folder
npm install launchdarkly-react-client-sdk
```

Modify main.tsx
```jsx
// add 
import { asyncWithLDProvider } from 'launchdarkly-react-client-sdk';

// ... existing code

// Wrap your App with LDProvider
const app = createRoot(document.getElementById('root')!);

(async () => {
	const LDProvider = await asyncWithLDProvider({
		clientSideID: 'YOUR_ENV_CLIENT_ID', // enter your LaunchDarkly environment client ID here
	});
app.render(
    <StrictMode>
        <Router>
            <ScrollToTop />
				<LDProvider>
					<App />
				</LDProvider>
        </Router>
		</StrictMode>
	);
})();
```

Modify the Header.tsx
```jsx
import { withLDConsumer } from 'launchdarkly-react-client-sdk';

// update 
export default withLDConsumer()(({flags}) => {
    // existing code
    {flags.featureCounter === true ? (
        <li className='relative flex flex-wrap just-fu-start m-0'>
            <NavLink to='/counter' className={navClass}>
                Counter
            </NavLink>
        </li>
    ) : null}
    // existing code
})

```

Go to the `Feature Counter` flag on LaunchDarkly and Toggle it on and serve `Available` to all traffic. 

You will notice how the `Counter` top nav is now visible in **realtime**. If you serve `Unavailable` then the menu item will disappear.