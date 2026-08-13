import express from 'express';
import { createServer as createViteServer } from 'vite';
import fs from 'node:fs/promises';
import { existsSync, accessSync, constants } from 'node:fs';
import { join } from 'node:path';
import { exec } from 'node:child_process';
import { promisify } from 'node:util';

const execAsync = promisify(exec);

function getDotnetExecutable(): string {
  const localDotnet = join(process.cwd(), '.dotnet', 'dotnet');
  try {
    if (existsSync(localDotnet)) {
      accessSync(localDotnet, constants.X_OK);
      return localDotnet;
    }
  } catch {
    // Permission denied or not executable, fallback to global dotnet
  }
  return 'dotnet';
}

async function startServer() {
  const app = express();
  const PORT = 3000;

  app.use(express.json({ limit: '50mb' }));

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
        contentApiKey: '',
        inputJsonPath: 'sample-ghost-export.json',
        outputDir: 'articles',
        indexFile: 'index.md',
        siteTitle: 'My Static Blog',
        includeDrafts: true,
        cleanUrls: false,
        quiet: false,
        downloadTheme: false,
        migrateTheme: false,
        purgeTemplate: false,
        disableAffix: false,
        themePath: '_ghost_templates/blogged-4.0.0',
        logoPath: false,
        googleAnalyticsTag: '',
        indexPostCount: 12,
        excerptMaxLength: 200
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
      const { customJson, config, useOfflineJson } = req.body;
      let tempJsonPath = '';

      if (useOfflineJson && customJson) {
        tempJsonPath = join(process.cwd(), 'temp-ghost-export.json');
        await fs.writeFile(tempJsonPath, typeof customJson === 'string' ? customJson : JSON.stringify(customJson, null, 2), 'utf-8');
      } else {
        if (config) {
          config.inputJsonPath = '';
          config.ghostExportJson = '';
        }
      }

      if (config) {
        const configPath = join(process.cwd(), 'ghostfx.json');
        if (tempJsonPath) {
          config.inputJsonPath = 'temp-ghost-export.json';
          config.ghostExportJson = 'temp-ghost-export.json';
        }
        await fs.writeFile(configPath, JSON.stringify(config, null, 2), 'utf-8');
      }

      const dotnetCmd = getDotnetExecutable();
      const cmd = `${dotnetCmd} run --project GhostFx.Cli -- --yes`;

      const { stdout, stderr } = await execAsync(cmd, { cwd: process.cwd(), input: '' });

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
      const dotnetCmd = getDotnetExecutable();
      const cmd = `${dotnetCmd} test GhostFx.Tests/GhostFx.Tests.csproj`;

      const { stdout, stderr } = await execAsync(cmd, { cwd: process.cwd(), input: '' });
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
      const files: any[] = [];

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

  const vite = await createViteServer({
    server: { middlewareMode: true },
    appType: 'spa',
  });

  app.use(vite.middlewares);

  app.use('*', async (req, res, next) => {
    const url = req.originalUrl;
    if (url.startsWith('/api/')) {
      return next();
    }
    try {
      const indexPath = join(process.cwd(), 'index.html');
      let template = await fs.readFile(indexPath, 'utf-8');
      template = await vite.transformIndexHtml(url, template);
      res.status(200).set({ 'Content-Type': 'text/html' }).send(template);
    } catch (e: any) {
      vite.ssrFixStacktrace(e);
      next(e);
    }
  });

  app.listen(PORT, '127.0.0.1', () => {
    console.log(`Express + Vite server running at http://localhost:${PORT}`);
  });
}

startServer();
