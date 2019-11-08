const currentDateUTC = new Date().toUTCString()

module.exports = {
	title: 'Azure Container Registry',
	dest: './gh-pages',
	base: '/acr/',
	markdown: {
		lineNumbers: true
	},
	themeConfig: {
		domain: 'http://azure.github.com',
		displayAllHeaders: true,
		sidebar: 'auto',
		searchMaxSuggestions: 10,
		repo: 'azure/acr',
		repoLabel: 'Star this Repo',
		editLinks: true,
		editLinkText: 'Edit this page on GitHub',
		logo: '/files/acr.svg',
		sidebar: [
            "/",
/*			{
				title: 'Home',
				collapsable: false,
				children: [
					'/'
				]
            }, 
            {
                title: 'Teleport',
                collapsable: false,
                children: ['/blog/teleport'],
            },
*/ 
            "/blog/teleport"
        ]
    }
}