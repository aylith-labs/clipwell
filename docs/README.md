# docs

This is a Next.js application generated with
[Create Fumadocs](https://github.com/fuma-nama/fumadocs).

It is a Next.js app with [Static Export](https://nextjs.org/docs/app/guides/static-exports) configured.

Run development server:

```bash
npm run dev
# or
pnpm dev
# or
yarn dev
```

Open http://localhost:3000 with your browser to see the result.

## Homepage and publishing paths

`/` and `/home` render the same full Clipwell homepage. Clipwell runs locally;
this public site provides its homepage, installation guide, and documentation.

The Pages workflow retains `/clipwell` for the default project domain. Before
changing its domain, build and review both root-mode production artifacts from the
exact source revision to deploy: feature docs with `PAGES_BASE=/` and engineering
with `PAGES_BASE=/engineering`. Verify homepage, `/home`, links, and assets in those
artifacts. **Deploy docs** also offers `custom_domain_preflight`: enable it to
build and upload both root-mode sites while skipping publication. Review that
run's exact revision and artifact before switching. A failed or interrupted build
blocks domain activation.

Keep the current domain and repository variable unchanged until those artifacts
are verified and a deployment is ready. During the controlled switch, bind
`clipwell.aylith.com` in **Settings → Pages → Custom domain**, set the repository
Actions variable `PAGES_CUSTOM_DOMAIN` to `clipwell.aylith.com`, and run **Deploy
docs** for that same reviewed revision. The workflow must succeed before adding
the DNS CNAME. Confirm HTTPS, both homepage routes, and engineering on the actual
custom host after DNS is in place. Do not bind the domain while a build is unproved.

Local production checks use `PAGES_BASE=/ npm run build` for the custom domain
and `PAGES_BASE=/clipwell npm run build` for project Pages. The engineering site
uses `PAGES_BASE=/engineering` or `PAGES_BASE=/clipwell/engineering` respectively.

Both sites bundle the same genuine Inter subsets from `assets/fonts/`, with the
original unicode ranges and fallback metrics. Their builds require no Google
Fonts request; the included OFL license covers those unchanged font files.

## Explore

In the project, you can see:

- `lib/source.ts`: Code for content source adapter, [`loader()`](https://fumadocs.dev/docs/headless/source-api) provides the interface to access your content.
- `lib/layout.shared.tsx`: Shared options for layouts, optional but preferred to keep.

| Route                     | Description                                            |
| ------------------------- | ------------------------------------------------------ |
| `app/(home)`              | The route group for your landing page and other pages. |
| `app/docs`                | The documentation layout and pages.                    |
| `app/api/search/route.ts` | The Route Handler for search.                          |

### Fumadocs MDX

A `source.config.ts` config file has been included, you can customise different options like frontmatter schema.

Read the [Introduction](https://fumadocs.dev/docs/mdx) for further details.

## Learn More

To learn more about Next.js and Fumadocs, take a look at the following
resources:

- [Next.js Documentation](https://nextjs.org/docs) - learn about Next.js
  features and API.
- [Learn Next.js](https://nextjs.org/learn) - an interactive Next.js tutorial.
- [Fumadocs](https://fumadocs.dev) - learn about Fumadocs
