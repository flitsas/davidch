This is the FLIT Identity frontend (Next.js App Router).

## FLIT Identity (local)

### Start stack

```bash
cd ../docker
cp .env.example .env   # if needed
docker compose up -d
```

- Frontend: http://localhost:3000
- API direct: http://localhost:5080/api/health
- Mailhog: http://localhost:8025

### Bootstrap login

- Email: `super@flit.local`
- Password: value of `IDENTITY_BOOTSTRAP_PASSWORD` in `docker/.env` (default `FlitDev2026!`)

Demo tenant (seeded on API startup when no tenants exist):

- Tenant admin: `admin@tenant-a.com` / `SecurePass!123`

### E2E tests

```bash
npm install
npx playwright install chromium
npx playwright test e2e/identity
```

Override credentials if your `docker/.env` differs:

```bash
E2E_ADMIN_EMAIL=super@flit.local E2E_ADMIN_PASSWORD=FlitDev2026! \
  npx playwright test e2e/identity
```

## Getting Started (dev only)

```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) with your browser to see the result.

You can start editing the page by modifying `app/page.tsx`. The page auto-updates as you edit the file.

This project uses [`next/font`](https://nextjs.org/docs/app/building-your-application/optimizing/fonts) to automatically optimize and load [Geist](https://vercel.com/font), a new font family for Vercel.

## Learn More

To learn more about Next.js, take a look at the following resources:

- [Next.js Documentation](https://nextjs.org/docs) - learn about Next.js features and API.
- [Learn Next.js](https://nextjs.org/learn) - an interactive Next.js tutorial.

You can check out [the Next.js GitHub repository](https://github.com/vercel/next.js) - your feedback and contributions are welcome!

## Deploy on Vercel

The easiest way to deploy your Next.js app is to use the [Vercel Platform](https://vercel.com/new?utm_medium=default-template&filter=next.js&utm_source=create-next-app&utm_campaign=create-next-app-readme) from the creators of Next.js.

Check out our [Next.js deployment documentation](https://nextjs.org/docs/app/building-your-application/deploying) for more details.
