import {
  AngularNodeAppEngine,
  createNodeRequestHandler,
  isMainModule,
  writeResponseToNodeResponse,
} from '@angular/ssr/node';
import express from 'express';
import {join} from 'node:path';
import {exec} from 'node:child_process';
import {promisify} from 'node:util';
import fs from 'node:fs/promises';
import {existsSync} from 'node:fs';

const execAsync = promisify(exec);
const browserDistFolder = join(import.meta.dirname, '../browser');

const app = express();
app.use(express.json({ limit: '50mb' }));

const angularApp = new AngularNodeAppEngine();

// GhostFx API Endpoints
app.get('/api/ghostfx/config', async (req, res) => {
  try {
    const configPath = join(process.cwd(), 'ghostfx.json');
    if (existsSync(configPath)) {
      const content = await fs.readFile(configPath, 'utf-8');
      return res.json(JSON.parse(content));
    }
    return res.json({
      ghostUrl: 'https://demo.ghost.io',
      adminApiKey: '640a1b2c3d4e5f6a7b8c9d0e:1234567890abcdef1234567890abcdef',
      inputJsonPath: 'sample-ghost-export.json',
      outputDir: 'articles',
      indexFile: 'index.md',
      siteTitle: 'My Static Blog',
      includeDrafts: true,
      downloadTheme: false,
      themeOutputPath: 'templates/ghost-theme.zip'
    });
  } catch (err: any) {
    return res.status(500).json({ error: err.message });
  }
});

app.post('/api/ghostfx/config', async (req, res) => {
  try {
    const configPath = join(process.cwd(), 'ghostfx.json');
    await fs.writeFile(configPath, JSON.stringify(req.body, null, 2), 'utf-8');
    return res.json({ success: true, config: req.body });
  } catch (err: any) {
    return res.status(500).json({ error: err.message });
  }
});

app.get('/api/ghostfx/sample-export', async (req, res) => {
  try {
    const samplePath = join(process.cwd(), 'sample-ghost-export.json');
    if (existsSync(samplePath)) {
      const content = await fs.readFile(samplePath, 'utf-8');
      return res.json(JSON.parse(content));
    }
    return res.status(404).json({ error: 'Sample export file not found' });
  } catch (err: any) {
    return res.status(500).json({ error: err.message });
  }
});

app.post('/api/ghostfx/migrate', async (req, res) => {
  try {
    const { customJson, config } = req.body;
    let tempJsonPath = '';

    if (customJson) {
      tempJsonPath = join(process.cwd(), 'temp-ghost-export.json');
      await fs.writeFile(tempJsonPath, typeof customJson === 'string' ? customJson : JSON.stringify(customJson, null, 2), 'utf-8');
    }

    if (config) {
      const configPath = join(process.cwd(), 'ghostfx.json');
      if (tempJsonPath) {
        config.inputJsonPath = 'temp-ghost-export.json';
      }
      await fs.writeFile(configPath, JSON.stringify(config, null, 2), 'utf-8');
    }

    const dotnetPath = join(process.cwd(), '.dotnet', 'dotnet');
    const cmd = `${dotnetPath} run --project GhostFx.Cli`;

    const { stdout, stderr } = await execAsync(cmd, { cwd: process.cwd() });
    
    return res.json({
      success: true,
      stdout,
      stderr
    });
  } catch (err: any) {
    return res.status(500).json({
      success: false,
      error: err.message,
      stdout: err.stdout || '',
      stderr: err.stderr || ''
    });
  }
});

app.post('/api/ghostfx/test', async (req, res) => {
  try {
    const dotnetPath = join(process.cwd(), '.dotnet', 'dotnet');
    const cmd = `${dotnetPath} test GhostFx.Tests/GhostFx.Tests.csproj`;

    const { stdout, stderr } = await execAsync(cmd, { cwd: process.cwd() });
    return res.json({
      success: true,
      stdout,
      stderr
    });
  } catch (err: any) {
    return res.status(500).json({
      success: false,
      error: err.message,
      stdout: err.stdout || '',
      stderr: err.stderr || ''
    });
  }
});

app.get('/api/ghostfx/files', async (req, res) => {
  try {
    const files: { path: string; name: string; isDraft: boolean; isIndex: boolean; isToc: boolean; isTag: boolean }[] = [];

    const scanDir = async (dir: string, relativePrefix: string = '') => {
      if (!existsSync(dir)) return;
      const entries = await fs.readdir(dir, { withFileTypes: true });
      for (const entry of entries) {
        const fullPath = join(dir, entry.name);
        const relPath = relativePrefix ? join(relativePrefix, entry.name) : entry.name;
        if (entry.isDirectory()) {
          await scanDir(fullPath, relPath);
        } else if (entry.name.endsWith('.md') || entry.name.endsWith('.yml')) {
          files.push({
            path: relPath,
            name: entry.name,
            isDraft: relPath.includes('drafts/'),
            isIndex: entry.name === 'index.md' || relPath === 'index.md',
            isToc: entry.name === 'toc.yml',
            isTag: relPath.includes('tags/') || relPath === 'tags.md'
          });
        }
      }
    };

    const articlesDir = join(process.cwd(), 'articles');
    await scanDir(articlesDir, 'articles');
    
    if (existsSync(join(process.cwd(), 'index.md'))) {
      files.push({ path: 'index.md', name: 'index.md', isDraft: false, isIndex: true, isToc: false, isTag: false });
    }
    if (existsSync(join(process.cwd(), 'tags.md'))) {
      files.push({ path: 'tags.md', name: 'tags.md', isDraft: false, isIndex: false, isToc: false, isTag: true });
    }

    return res.json({ files });
  } catch (err: any) {
    return res.status(500).json({ error: err.message });
  }
});

app.get('/api/ghostfx/file-content', async (req, res) => {
  try {
    const filePath = req.query['path'] as string;
    if (!filePath) {
      return res.status(400).json({ error: 'path parameter required' });
    }
    const fullPath = join(process.cwd(), filePath);
    if (!existsSync(fullPath)) {
      return res.status(404).json({ error: 'File not found' });
    }
    const content = await fs.readFile(fullPath, 'utf-8');
    return res.json({ path: filePath, content });
  } catch (err: any) {
    return res.status(500).json({ error: err.message });
  }
});

/**
 * Serve static files from /browser
 */
app.use(
  express.static(browserDistFolder, {
    maxAge: '1y',
    index: false,
    redirect: false,
  }),
);

/**
 * Handle all other requests by rendering the Angular application.
 */
app.use((req, res, next) => {
  angularApp
    .handle(req)
    .then((response) =>
      response ? writeResponseToNodeResponse(response, res) : next(),
    )
    .catch(next);
});

/**
 * Start the server if this module is the main entry point, or it is ran via PM2.
 * The server listens on the port defined by the `PORT` environment variable, or defaults to 4000.
 */
if (isMainModule(import.meta.url) || process.env['pm_id']) {
  const port = process.env['PORT'] || 4000;
  app.listen(port, (error) => {
    if (error) {
      throw error;
    }

    console.log(`Node Express server listening on http://localhost:${port}`);
  });
}

/**
 * Request handler used by the Angular CLI (for dev-server and during build) or Firebase Cloud Functions.
 */
export const reqHandler = createNodeRequestHandler(app);
